using System;
using System.Linq;
using CMSlib;
using FifaTemplates;
using FifaEditorial;
using Editorial.Entities;
using Editorial.Web;
using Editorial.Web.Entities;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Data.SqlClient;
using FifaTemplates.Slots;
using System.Collections;
using System.Xml;
using System.Web;
using D3EngageConnector.Models;
using FifaEngageConnector;
using FifaEngageConnector.Models;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Text;
using FifaCommonUtils;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using ModuleSocial;

namespace fifa.user
{
  public class msLoginHandler : MasterSlotNoView
  {

    [CmsPar("")]
    public string state;
    [CmsPar("")]
    public string id_token;
    [CmsPar("")]
    public string code;
    [CmsPar("false")]
    public string debug;
    [CmsPar("")]
    public string error;
    [CmsPar("")]
    public string error_description;
    [CmsPar("false")]
    public string logout;

    private Dictionary<string, string> _props = new Dictionary<string, string>();
    private Dictionary<string, string> _propsMapping = new Dictionary<string, string>();
    private List<string> _cookieProps = new List<string>();

    private B2CLoginService _b2cLoginService = new B2CLoginService();

    protected override void ExecutePage()
    {
      string _callResult = string.Empty;
      string encodedState = state;

      if (!string.IsNullOrEmpty(error))
      {
        WriteDebugInfo(string.Format("error {0}", error));
        switch (error)
        {
          case "access_denied":
            WriteDebugInfo(string.Format("error_description {0}", error_description));
            if (!string.IsNullOrEmpty(error_description) && error_description.Contains("AADB2C90118"))
            {
              string msForgotPasswordUrl = CommonUtils.GetMicrosoftForgotPasswordUrl(state);
              WriteDebugInfo(string.Format("forgotPasswordUrl {0}", msForgotPasswordUrl));
              Response.Redirect(msForgotPasswordUrl);
            }
            break;
          default:
            WriteDebugInfo(string.Format("error_description {0}", error_description));
            break;
        }
      }


      try
      {


        if (string.IsNullOrEmpty(state))
        {
          throw new Exception("state parameter is missing");
        }


        state = CommonUtils.DecodeBase64(state);
        WriteDebugInfo(string.Format("state {0}", state));

        if (logout.Trim().ToLower().Equals("true"))
        {
          string _logoutAddress = CommonUtils.getWebSettingByEnv("MicrosoftLogoutEndpoint");
          state = state.Replace("#login", "").Replace("#registration", "");
          state = string.Concat(state, (state.IndexOf("?") == -1 ? "?" : "&"), "rnd=", template.RequestPar("rnd"));
          _logoutAddress += "&post_logout_redirect_uri=" + state;

          //WriteToLogFile(string.Format("LOGOUT TO: {0}", _logoutAddress));

          if (Request.Cookies["FIFACom"] != null)
          {
            HttpCookie myCookie = new HttpCookie("FIFACom");
            myCookie.Expires = DateTime.Now.AddDays(-1d);
            myCookie.Domain = CommonUtils.getWebSettingByEnv("CookieDomain");
            Response.Cookies.Add(myCookie);
          }

          Response.Redirect(_logoutAddress);
        }

        if (string.IsNullOrEmpty(code))
        {
          if (!string.IsNullOrEmpty(state))
          {
            // removing tracking information
            state = state.Replace("#login", "").Replace("#registration", "");

            //Response.Redirect(state);
            Response.Write("<meta http-equiv=\"refresh\" content=\"0;URL=" + state + "\">");
            //Response.End();
            //HttpContext.Current.Response.Clear();
            //HttpContext.Current.Response.StatusCode = 302;
            //HttpContext.Current.Response.AddHeader("Location", state);
            //HttpContext.Current.Response.End();

          }
          else
          {
            //WriteToLogFile("code parameter is missing");
            throw new Exception("code parameter is missing");
          }
        }

        else
        {
          WriteDebugInfo(string.Format("code {0}", code));

          FIFAComUser _user = _b2cLoginService.GetUserBy_B2C_AuthCode(code);
          if (!string.IsNullOrEmpty(_user.IdToken))
          {
            try
            {
              JObject obj = JsonConvert.DeserializeObject<JObject>(JWT.JsonWebToken.Decode(_user.IdToken, string.Empty, false));
              string activityReport = obj.GetValue("ActivityReport").ToString();
              if (!string.IsNullOrEmpty(activityReport))
              {
                string[] activityReportParts = activityReport.Split(',');
                switch (activityReportParts[0].ToLower())
                {
                  case "sign-in":
                    state = string.Concat(state.Replace("#login", "").Replace("#registration", ""), "#login");
                    break;
                  case "sign-up":
                    state = string.Concat(state.Replace("#login", "").Replace("#registration", ""), "#registration");
                    break;
                }
              }
            }
            catch (Exception) { }
          }

          if (_user.ErrorResponse != null)
          {
            switch (_user.ErrorResponse.error)
            {
              case "invalid_grant":
                //AADB2C90088: The provided grant has not been issued for this endpoint.
                //temporary workaround for this error code to be sure user at least get redirected to login page
                if (_user.ErrorResponse.error_description.Contains("AADB2C90088"))
                {
                  Response.Redirect(CommonUtils.GetMicrosoftLoginUrl(encodedState));

                }
                break;
              default:
                throw _user.ErrorResponse.ThrowableException;
            }
          }
          WriteDebugInfo(string.Format("FIFAComId {0}", _user.FIFAComId));
          WriteDebugInfo(string.Format("_cookieValue {0}", _user.ToCookieString()));

          _b2cLoginService.UpdateFIFAComCookie(_user);

          if (state.EndsWith("error.html"))
          {
            state = state.Replace("error.html", "index.html");
          }

          WriteDebugInfo(string.Format("saved to mongo DB"));

          if (!debug.ToLower().Equals("true"))
          {
            //Response.Redirect(state);
            Response.Write("<meta http-equiv=\"refresh\" content=\"0;URL=" + state + "\">");
            //Response.End();
            //HttpContext.Current.Response.Clear();
            //HttpContext.Current.Response.StatusCode = 302;
            //HttpContext.Current.Response.AddHeader("Location", state);
            //HttpContext.Current.Response.End();
          }
        }

      }
      catch (Exception ex)
      {
        if (debug.ToLower().Equals("true"))
        {
          WriteDebugInfo(string.Format("exepction: {0}", ex));
        }
        else
        {
          //WriteToLogFile(string.Format("{0} - {1}", ex.Message, ex.StackTrace));
          throw ex;
        }
      }

    }

    private void WriteDebugInfo(string text)
    {
      if (debug.ToLower().Equals("true"))
      {
        Response.Write(text + "<br /><br />");
      }
      _b2cLoginService.WriteToLogFile(text);
    }
  }
}

