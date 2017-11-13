// V2
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// The following using statements were added for this sample.
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security.Cookies;
using System.Security.Claims;
using System.Configuration;
using System.Net;

namespace Microsoft.Mdp.Identity.Demo.Web.Controllers
{
    public class AccountController : Controller
    {
        public void Login()
        {
            var state = Request.Form["state"];
            var code = Request.Form["code"];

            if (!Request.IsAuthenticated)
            {
                var properties = new AuthenticationProperties
                {
                    RedirectUri = "/"                    
                };

                // The state is what you pass to the AuthenticationProperties.Dictionary during authentication challenge
                //properties.Dictionary["UILanguage"] = "English";

                // To execute a policy, you simply need to trigger an OWIN challenge.
                // You can indicate which policy to use by specifying the policy id as the AuthenticationType
                HttpContext.GetOwinContext().Authentication.Challenge(properties, Startup.SusiPolicyId);
            }
        }

        public void UpdateProfile()
        {
            // App config settings
            string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            string aadInstance = ConfigurationManager.AppSettings["ida:AadInstance"];
            string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
            string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];

            int i = aadInstance.IndexOf("{");
            aadInstance = aadInstance.Substring(0, i);

            redirectUri = WebUtility.UrlEncode(redirectUri);

            if (Request.IsAuthenticated)
            {
                string EditProfile = ConfigurationManager.AppSettings["ida:EditProfile"];
                HttpContext.Response.Redirect($"{aadInstance}{tenant}/oauth2/v2.0/authorize?p={EditProfile}&client_id={clientId}&redirect_uri={redirectUri}&response_mode=form_post&response_type=id_token&scope=openid&state=");
            }
        }
        public void ResetPassword()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties() { RedirectUri = "/" }, Startup.PasswordResetPolicyId);
            }
        }
        public void Logout()
        {
            // To sign out the user, you should issue an OpenIDConnect sign out request.
            if (Request.IsAuthenticated)
            {
                IEnumerable<AuthenticationDescription> authTypes = HttpContext.GetOwinContext().Authentication.GetAuthenticationTypes();
                HttpContext.GetOwinContext().Authentication.SignOut(authTypes.Select(t => t.AuthenticationType).ToArray());
                Request.GetOwinContext().Authentication.GetAuthenticationTypes();
            }
            else
            {
                // FIFA: if user clicks on the logout, but for some reason the authentication
                // is expired, redirect the user back to Startup.redirectUri
                HttpContext.Response.Redirect(Startup.redirectUri);
            }
        }
    }
}