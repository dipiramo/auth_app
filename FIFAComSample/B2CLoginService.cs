using System.Collections.Generic;
using System.Data;
using FifaDataLayer;
using FifaTypes;
using ModuleSocial.Models;
using System;
using System.Configuration;
using FifaEngageConnector.Models;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using FifaEngageConnector;
using MongoDB.Driver;
using FifaCommonUtils;
using MongoDB.Bson;
using System.Linq;
using System.IO;
using System.Web;
using CMSlib;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace ModuleSocial
{
  public class B2CLoginService
  {

    protected static IMongoClient _client;
    protected static IMongoDatabase _database;

	  private TelemetryClient _aiTelemetryClient;

	  public B2CLoginService()
	  {
		  //string iKey = ConfigurationManager.AppSettings["Logging:ApplicationInsights:InstrumentationKey"];
			string iKey = "6c7d4154-0f9b-4a80-9efe-2989b5f9a4bc";
			if (string.IsNullOrWhiteSpace(iKey))
			  TelemetryConfiguration.Active.DisableTelemetry = true;
		  else
		  {
			  TelemetryConfiguration.Active.InstrumentationKey = iKey;

			  _aiTelemetryClient = new TelemetryClient();
			  _aiTelemetryClient.Context.User.Id = Environment.UserName;
			  _aiTelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
			  _aiTelemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

		  }
	  }


    public FIFAComUser GetUserBy_B2C_AuthCode(string authCode)
    {

      string _address = CommonUtils.getWebSettingByEnv("MicrosoftAccessTokenEndpoint");
      string _clientId = CommonUtils.getWebSettingByEnv("MicrosoftLoginClientID");
      string _clientSecret = CommonUtils.getWebSettingByEnv("MicrosoftClientSecret");
      string _redirectUri = CommonUtils.getWebSettingByEnv("MicrosoftLoginRedirectUri");



	    _aiTelemetryClient.Context.Properties["authCode"] = authCode;
	    _aiTelemetryClient.Context.Properties["callAddress"] = _address;
	    
      FIFAComUser _user = new FIFAComUser();

	    using (var operation = _aiTelemetryClient.StartOperation<RequestTelemetry>("GetUserBy_B2C_AuthCode"))
	    {

				var exProps = new Dictionary<string, string>();

		    try
		    {

			    string _callResult = "";

			    var _params = new System.Collections.Specialized.NameValueCollection();
			    _params.Add("grant_type", "authorization_code");
			    _params.Add("client_id", _clientId);
			    _params.Add("code", authCode);
			    _params.Add("scope", "openid offline_access");
			    _params.Add("client_secret", _clientSecret);


					
				  exProps.Add("params.grant_type", "authorization_code");
					exProps.Add("params.client_id", _clientId);
					exProps.Add("params.code", authCode);
					exProps.Add("params.scope", "openid offline_access");
					exProps.Add("params.client_secret", _clientSecret);


			    _aiTelemetryClient.Context.Properties["GetUserByB2CAuthCodeCallParams"] = JsonConvert.SerializeObject(_params);
			    using (WebClient _wc = new WebClient())
			    {
				    _wc.Encoding = Encoding.UTF8;
				    _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
						byte[] _responseBytes = _wc.UploadValues(_address, _params);
				    _callResult = Encoding.UTF8.GetString(_responseBytes);
			    }

					_aiTelemetryClient.Context.Properties["GetUserByB2CAuthCodeCallResult"] = _callResult;
			    FIFAComUserB2CAccessToken _accessToken = GetAccessTokenFromJSon(_callResult);
					_aiTelemetryClient.Context.Properties["GetUserByB2CAuthCodeAccessTokenObject"] = JsonConvert.SerializeObject(_accessToken);
			    _user = this.GetUserBy_B2C_Login_IdToken(_accessToken.IdToken);

			    this.SaveAccessToken(_user.FIFAComId, _user.Email, _accessToken);

		    }
		    catch (WebException ex)
		    {

					_aiTelemetryClient.TrackException(ex, exProps);
			    
					string ex_response =
				    WriteToLogFile(
					    string.Format("Error in GetUserBy_B2C_AuthCode (with authcode: {2}): {0} - {1}", ex.Message, ex.StackTrace,
						    authCode), ex);
			    _user.ErrorResponse = JsonConvert.DeserializeObject<B2CErrorResponse>(ex_response);
			    _user.ErrorResponse.ThrowableException =
				    new WebException(string.Format("Error on GetUserBy_B2C_AuthCode. Response received: {0}", ex_response));
		    }
		    
	    }

	    return _user;
    }

	  public FIFAComUser GetUserBy_B2C_Login_FIFAComId(string fifaComId)
	  {
		  using (var operation =
			  _aiTelemetryClient.StartOperation<RequestTelemetry>(String.Concat("GetUserBy_B2C_Login_FIFAComId")))
		  {
			  FIFAComUserB2CAccessToken _accessToken = this.GetLatestAccessToken(fifaComId);

			  _aiTelemetryClient.Context.Properties["fifaComId"] = fifaComId;
			  _aiTelemetryClient.Context.Properties["accessToken"] = JsonConvert.SerializeObject(_accessToken);

			  if (!_accessToken.IsStillValid())
			  {
				  _accessToken = this.RefreshAccessToken(_accessToken);
			  }
			
			  return this.GetUserBy_B2C_Login_IdToken(_accessToken.IdToken);
		  }

	  }

	  public FIFAComUser GetUserBy_B2C_Login_IdToken(string idToken)
    {

      FIFAComUser _user = new FIFAComUser();

	    using (var operation =
		    _aiTelemetryClient.StartOperation<RequestTelemetry>("GetUserBy_B2C_Login_IdToken"))
	    {

		    string _address = CommonUtils.getWebSettingByEnv("MicrosoftAPI.FanGet.URL");

		    try
		    {

			    string _callResult = string.Empty;
			    _aiTelemetryClient.Context.Properties["GetUserBy_B2C_Login_IdToken address call"] = _address;
			    _aiTelemetryClient.Context.Properties["GetUserBy_B2C_Login_IdToken call Token"] = string.Format("Bearer {0}", idToken);
		
			    using (WebClient _wc = new WebClient())
			    {
				    _wc.Encoding = Encoding.UTF8;
				    _wc.Headers.Add(HttpRequestHeader.Authorization, string.Format("Bearer {0}", idToken));
				    _callResult = _wc.DownloadString(_address);
			    }
			    _aiTelemetryClient.Context.Properties["GetUserBy_B2C_Login_IdToken - callResult"] = _callResult;
			    _user = FIFAComUmpWrapper.GetUserFromJSon(_callResult, idToken);
			    _user.IdToken = idToken;
			    _aiTelemetryClient.Context.Properties["GetUserBy_B2C_Login_IdToken - FIFAComUser"] = JsonConvert.SerializeObject(_user);

		    }
		    catch (WebException ex)
		    {
			    _aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(
				    string.Format("Error in GetUserBy_B2C_Login_IdToken (with token: {2}): {0} - {1}", ex.Message, ex.StackTrace,
					    idToken), ex);
			    throw new Exception("Error on GetUserBy_B2C_Login_IdToken", ex);
		    }
	    }
	    return _user;
	    
    }

    public bool UpdateUser_B2C(FIFAComUser user)
    {

      bool _updateOk = false;
      string idToken = "";

	    using (var operation =
		    _aiTelemetryClient.StartOperation<RequestTelemetry>("UpdateUser_B2C"))
	    {

		    try
		    {
			    FIFAComUserB2CAccessToken _accessToken = this.GetLatestAccessToken(user.FIFAComId);

			    string _address = CommonUtils.getWebSettingByEnv("MicrosoftAPI.FanUpdate.URL");
			    string _clientId = CommonUtils.getWebSettingByEnv("MicrosoftLoginClientID");
			    string _clientSecret = CommonUtils.getWebSettingByEnv("MicrosoftClientSecret");
			    string _redirectUri = CommonUtils.getWebSettingByEnv("MicrosoftLoginRedirectUri");
			    ;

			    if (!_accessToken.IsStillValid())
			    {
				    _accessToken = this.RefreshAccessToken(_accessToken);
			    }

			    idToken = _accessToken.IdToken;

			    string _callResult = string.Empty;
			    _aiTelemetryClient.Context.Properties["UpdateUser_B2C address call"] = _address;
			    _aiTelemetryClient.Context.Properties["UpdateUser_B2C call Token"] = JsonConvert.SerializeObject(_accessToken);
			    _aiTelemetryClient.Context.Properties["UpdateUser_B2C params"] = JsonConvert.SerializeObject(user.To_B2C_JObject());

			    using (WebClient _wc = new WebClient())
			    {
				    _wc.Encoding = Encoding.UTF8;
				    _wc.Headers.Add("Context-Behavior", "Synchronous");
				    _wc.Headers[HttpRequestHeader.ContentType] = "application/json";
				    _wc.Headers[HttpRequestHeader.Authorization] = "Bearer " + _accessToken.IdToken;
		
				    _callResult = _wc.UploadString(_address, user.To_B2C_JObject().ToString());

			    }

			    UpdateFIFAComCookie(user);

			    _updateOk = true;

		    }
		    catch (WebException ex)
		    {
			    _aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(
				    string.Format("Error in UpdateUser_B2C (With Token {2}): {0} - {1}", ex.Message, ex.StackTrace, idToken), ex);
		    }
	    }
	    return _updateOk;
	    
    }

    public bool DeleteUser_B2C(string fifaComId)
    {

      bool _updateOk = false;
      string idToken = "";
	    using (var operation =
		    _aiTelemetryClient.StartOperation<RequestTelemetry>("DeleteUser_B2C"))
	    {
		    try
		    {
			    FIFAComUserB2CAccessToken _accessToken = this.GetLatestAccessToken(fifaComId);

			    string _address = CommonUtils.getWebSettingByEnv("MicrosoftAPI.FanDelete.URL");
			    string _clientId = CommonUtils.getWebSettingByEnv("MicrosoftLoginClientID");
			    string _clientSecret = CommonUtils.getWebSettingByEnv("MicrosoftClientSecret");
			    string _redirectUri = CommonUtils.getWebSettingByEnv("MicrosoftLoginRedirectUri");

			    if (!_accessToken.IsStillValid())
			    {
				    _accessToken = this.RefreshAccessToken(_accessToken);
			    }

			    idToken = _accessToken.IdToken;

			    var request = WebRequest.Create(_address);
			    request.Method = "DELETE";
			    request.Headers.Add("Context-Behavior", "Synchronous");
			    request.ContentType = "application/json";
			    request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _accessToken.IdToken;

					_aiTelemetryClient.Context.Properties["DeleteUser_B2C address call"] = _address;
					_aiTelemetryClient.Context.Properties["DeleteUser_B2C call Token"] = JsonConvert.SerializeObject(_accessToken);
					

			    var response = (HttpWebResponse) request.GetResponse();

			    _updateOk = true;

		    }
		    catch (WebException ex)
		    {
			    _aiTelemetryClient.TrackException(ex);
			    string ex_response = string.Empty;
			    using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream()))
			    {
				    ex_response = sr.ReadToEnd();
				    sr.Dispose();
			    }
			    B2CExceptionWebResponse b2cWebException = JsonConvert.DeserializeObject<B2CExceptionWebResponse>(ex_response);
			    WriteToLogFile(string.Format("Error in DeleteUser_B2C (userid: {0}\r\ntoken: {1})\r\nWebResponse: {2}",
				    fifaComId, idToken, b2cWebException.Message));
		    }
	    }
	    return _updateOk;
	    
    }

    public void UpdateFIFAComCookie(FIFAComUser user)
    {
	    using (var operation =
				_aiTelemetryClient.StartOperation<RequestTelemetry>("UpdateFIFAComCookie"))
	    {
		    string _cookieValue = user.ToCookieString();

		    if (string.IsNullOrEmpty(_cookieValue))
		    {
			    throw new Exception("Cookie value is empty");
		    }

		    HttpCookie _fifacomCookie = new HttpCookie("FIFACom");
		    _fifacomCookie.Value = _cookieValue;
		    _fifacomCookie.Path = "/";
		    _fifacomCookie.Domain = CommonUtils.getWebSettingByEnv("CookieDomain");
		    CmsTrace.Trace("CookieDomain: " + CommonUtils.getWebSettingByEnv("CookieDomain"), "B2CLoginService", 0, 0, 0,
			    config.GetConnString(7));
		    if (HttpContext.Current.Request.Url.AbsoluteUri.ToLower().Contains("localhost"))
		    {
			    _fifacomCookie.Domain = "localhost";
		    }

		    _fifacomCookie.Expires = DateTime.Now.AddDays(14);
		    HttpContext.Current.Response.Cookies.Set(_fifacomCookie);
	    }
    }

    private void SaveAccessToken(string fifacomid, string userEmail, FIFAComUserB2CAccessToken accessToken)
    {
	    using (var operation =
				_aiTelemetryClient.StartOperation<RequestTelemetry>("SaveAccessToken"))
	    {

		    _aiTelemetryClient.Context.Properties["fifaComId"] = fifacomid;
				_aiTelemetryClient.Context.Properties["accessToken"] = JsonConvert.SerializeObject(accessToken);
				_aiTelemetryClient.Context.Properties["userEmail"] = userEmail;


		    if (string.IsNullOrEmpty(fifacomid) || string.IsNullOrEmpty(userEmail))
		    {
			    return;
		    }

		    try
		    {

			    _client = new MongoClient(CommonUtils.getWebSettingByEnv("AzureMongo.ConnectionString"));
			    _database = _client.GetDatabase(CommonUtils.getWebSettingByEnv("AzureMongo.UserManagementDB"));
			    var _collection = _database.GetCollection<BsonDocument>("UserTokens");

			    var document = new BsonDocument
			    {
				    {"FIFAComId", fifacomid},
				    {"Email", userEmail},
				    {"IdToken", accessToken.IdToken},
				    {"IdTokenExpiresIn", accessToken.IdTokenExpiresIn},
				    {"NotBefore", accessToken.NotBefore},
				    {"ProfileInfo", accessToken.ProfileInfo},
				    {"RefreshToken", accessToken.RefreshToken},
				    {"RefreshTokenExpiresIn", accessToken.RefreshTokenExpiresIn},
				    {"LastUpdateDate", DateTime.UtcNow}
			    };
			    var _builder = Builders<BsonDocument>.Filter;
			    var _filter = _builder.Eq("FIFAComId", fifacomid);
			    List<BsonDocument> _result = _collection.Find(_filter).ToList();

			    if (_result.Count > 0)
			    {
				    var update = Builders<BsonDocument>.Update
					    .Set("IdToken", accessToken.IdToken)
					    .Set("IdTokenExpiresIn", accessToken.IdTokenExpiresIn)
					    .Set("NotBefore", accessToken.NotBefore)
					    .Set("ProfileInfo", accessToken.ProfileInfo)
					    .Set("RefreshToken", accessToken.RefreshToken)
					    .Set("RefreshTokenExpiresIn", accessToken.RefreshTokenExpiresIn)
					    .Set("LastUpdateDate", DateTime.UtcNow);
				    var updateResult = _collection.UpdateOne(_filter, update, new UpdateOptions {IsUpsert = true});
			    }
			    else
			    {
				    _collection.InsertOneAsync(document);
			    }

		    }
		    catch (WebException ex)
		    {
					_aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(string.Format("Error in SaveAccessToken: {0} - {1}", ex.Message, ex.StackTrace), ex);
			    throw new Exception("Error on SaveAccessToken", ex);
		    }
	    }

    }

    public FIFAComUserB2CAccessToken RefreshAccessToken(FIFAComUserB2CAccessToken oldToken)
    {

      FIFAComUserB2CAccessToken _accessToken = new FIFAComUserB2CAccessToken();

	    using (var operation =
		    _aiTelemetryClient.StartOperation<RequestTelemetry>("RefreshAccessToken"))
	    {

		    string _address = CommonUtils.getWebSettingByEnv("MicrosoftAccessTokenEndpoint");
		    string _clientId = CommonUtils.getWebSettingByEnv("MicrosoftLoginClientID");
		    string _clientSecret = CommonUtils.getWebSettingByEnv("MicrosoftClientSecret");
		    string _redirectUri = CommonUtils.getWebSettingByEnv("MicrosoftLoginRedirectUri");

		    try
		    {

			    string _callResult = "";
			    string _params =
				    string.Format(
					    "grant_type=refresh_token&client_id={0}&refresh_token={1}&redirect_uri={2}&scope=openid offline_access&client_secret={3}",
					    _clientId, oldToken.RefreshToken, _redirectUri, _clientSecret);
			 
					_aiTelemetryClient.Context.Properties["RefreshAccessToken address call"] = _address;
			    _aiTelemetryClient.Context.Properties["RefreshAccessToken call params"] = _params;

			    using (WebClient _wc = new WebClient())
			    {
				    _wc.Encoding = Encoding.UTF8;
				    _wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

	
				    _callResult = _wc.UploadString(_address, _params);
			    }

			    _accessToken = GetAccessTokenFromJSon(_callResult);

			    this.SaveAccessToken(oldToken.FIFAComId, oldToken.Email, _accessToken);

		    }
		    catch (WebException ex)
		    {
			    _aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(string.Format("Error in RefreshAccessToken: {0} - {1}", ex.Message, ex.StackTrace), ex);
			    throw new Exception("Error on RefreshAccessToken", ex);
		    }
	    }
	    return _accessToken;
	    
    }

    public FIFAComUserB2CAccessToken GetLatestAccessToken(string fifacomid)
    {

      FIFAComUserB2CAccessToken _accessToken = new FIFAComUserB2CAccessToken();

	    using (var operation =
		    _aiTelemetryClient.StartOperation<RequestTelemetry>("GetLatestAccessToken"))
	    {

		    try
		    {

			    _client = new MongoClient(CommonUtils.getWebSettingByEnv("AzureMongo.ConnectionString"));
			    _database = _client.GetDatabase(CommonUtils.getWebSettingByEnv("AzureMongo.UserManagementDB"));
			    var _collection = _database.GetCollection<BsonDocument>("UserTokens");

			    List<BsonDocument> _result = new List<BsonDocument>();
			    var _builder = Builders<BsonDocument>.Filter;
			    var _filter = _builder.Eq("FIFAComId", fifacomid);
			    var _sort = Builders<BsonDocument>.Sort.Ascending("FIFAComId").Ascending("numorder");


			    _result = _collection.Find(_filter).Sort(_sort).ToList();

			    //BsonDocument _result = _collection.Find(_filter).ToBsonDocument();

			    foreach (BsonDocument _row in _result)
			    {
				    List<BsonElement> _elements = (List<BsonElement>) _row.Elements;

				    _accessToken.FIFAComId = (string) ((BsonElement) (from e in _elements where e.Name == "FIFAComId" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.Email = (string) ((BsonElement) (from e in _elements where e.Name == "Email" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.IdToken = (string) ((BsonElement) (from e in _elements where e.Name == "IdToken" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.IdToken = (string) ((BsonElement) (from e in _elements where e.Name == "IdToken" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.IdTokenExpiresIn =
					    (string) ((BsonElement) (from e in _elements where e.Name == "IdTokenExpiresIn" select e).FirstOrDefault())
					    .Value;
				    _accessToken.NotBefore = (string) ((BsonElement) (from e in _elements where e.Name == "NotBefore" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.ProfileInfo = (string) ((BsonElement) (from e in _elements where e.Name == "ProfileInfo" select e)
					    .FirstOrDefault()).Value;
				    _accessToken.RefreshToken =
					    (string) ((BsonElement) (from e in _elements where e.Name == "RefreshToken" select e).FirstOrDefault()).Value;
				    _accessToken.RefreshTokenExpiresIn =
					    (string) ((BsonElement) (from e in _elements where e.Name == "RefreshTokenExpiresIn" select e)
						    .FirstOrDefault()).Value;
				    BsonElement elm = (from e in _elements where e.Name == "LastUpdateDate" select e).FirstOrDefault();
				    if (elm != null && elm.Value != null)
				    {
					    _accessToken.LastUpdateDate = (DateTime) elm.Value;
				    }
			    }
					_aiTelemetryClient.Context.Properties["GetLatestAccessToken accessToken"] = JsonConvert.SerializeObject(_accessToken);


		    }
		    catch (WebException ex)
		    {
			    _aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(string.Format("Error in GetLatestAccessToken: {0} - {1}", ex.Message, ex.StackTrace), ex);
			    throw new Exception("Error on GetLatestAccessToken", ex);
		    }
	    }
	    return _accessToken;
	    
    }

    private FIFAComUserB2CAccessToken GetAccessTokenFromJSon(string inputJSon)
    {

      FIFAComUserB2CAccessToken _accessToken = new FIFAComUserB2CAccessToken();
	    using (var operation =
				_aiTelemetryClient.StartOperation<RequestTelemetry>("GetAccessTokenFromJSon"))
	    {

		    _aiTelemetryClient.Context.Properties["GetAccessTokenFromJSon - inputJSon"] = inputJSon;
		    try
		    {
			    JObject _accessTokenJSon = JsonConvert.DeserializeObject<JObject>(inputJSon);

			    _accessToken.IdToken = _accessTokenJSon.GetValue("id_token").ToString();
			    _accessToken.TokenType = _accessTokenJSon.GetValue("token_type").ToString();
			    _accessToken.NotBefore = _accessTokenJSon.GetValue("not_before").ToString();
			    _accessToken.IdTokenExpiresIn = _accessTokenJSon.GetValue("id_token_expires_in").ToString();
			    _accessToken.ProfileInfo = _accessTokenJSon.GetValue("profile_info").ToString();
			    _accessToken.RefreshToken = _accessTokenJSon.GetValue("refresh_token").ToString();
			    _accessToken.RefreshTokenExpiresIn = _accessTokenJSon.GetValue("refresh_token_expires_in").ToString();
			    _accessToken.LastUpdateDate = DateTime.UtcNow;

			    _aiTelemetryClient.Context.Properties["GetAccessTokenFromJSon - inputJSon object"] = JsonConvert.SerializeObject(_accessTokenJSon);
		    }
		    catch (WebException ex)
		    {
					_aiTelemetryClient.TrackException(ex);
			    WriteToLogFile(string.Format("Error in GetAccessTokenFromJSon: {0} - {1}", ex.Message, ex.StackTrace), ex);
			    throw new Exception("Error on GetAccessTokenFromJSon", ex);
		    }
	    }
	    return _accessToken;
    }

    public string WriteToLogFile(string text, WebException ex = null)
		{
			string ex_response = string.Empty;
	    using (var operation =
				_aiTelemetryClient.StartOperation<RequestTelemetry>("WriteToLogFile"))
	    {

		    if (CommonUtils.getWebSettingByEnv("Microsoft.Disable.TextLog").Equals("true"))
		    {
			    return null;
		    }
		   
		    if (ex != null)
		    {
			    using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream()))
			    {
				    ex_response = sr.ReadToEnd();
				    sr.Dispose();
			    }
			    text = string.Concat(text, " ", ex_response);
		    }
		    string logFilePath = string.Format("{0}\\b2cLogin_Logs\\Log_Service_{1}.txt",
			    CommonUtils.getWebSettingByEnv("codeFolder"), DateTime.Now.ToString("yyyy_MM_dd"));

		    if (File.Exists(logFilePath))
		    {
			    FileInfo fi = new FileInfo(logFilePath);
			    if (fi.Length > 2014000)
			    {
				    fi.MoveTo(string.Format("{0}\\b2cLogin_Logs\\Log_Service_{1}.txt", CommonUtils.getWebSettingByEnv("codeFolder"),
					    DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")));
			    }
		    }

		    using (StreamWriter sw = new StreamWriter(logFilePath, true))
		    {
			    sw.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss"), text));
		    }
	    }
	    return ex_response;
    }

  }
}
