﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// The following using statements were added for this sample
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Protocols;
using System.Web.Mvc;
using System.Configuration;
using System.IdentityModel.Tokens;

namespace Microsoft.Mdp.Identity.Demo.Web
{
    public partial class Startup
    {
        // App config settings
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AadInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        public static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        public static string secretId = ConfigurationManager.AppSettings["ida:SecretId"];

        // B2C policy identifiers
        public static string SusiPolicyId = ConfigurationManager.AppSettings["ida:SusiPolicyId"];
        public static string PasswordResetPolicyId = ConfigurationManager.AppSettings["ida:PasswordResetPolicyId"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                CookieDomain = "localhost"
            });

            // Configure OpenID Connect middleware for each policy
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(PasswordResetPolicyId));
            app.UseOpenIdConnectAuthentication(CreateOptionsFromPolicy(SusiPolicyId));
            app.UseOAuthBearerTokens(new Owin.Security.OAuth.OAuthAuthorizationServerOptions
            {

            });
        }

        // Used for avoiding yellow-screen-of-death TODO
        private Task AuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();

            if (notification.ProtocolMessage.ErrorDescription != null && notification.ProtocolMessage.ErrorDescription.Contains("AADB2C90118"))
            {
                // If the user clicked the reset password link, redirect to the reset password route
                notification.Response.Redirect("/Account/ResetPassword");
            }
            else if (notification.Exception.Message == "access_denied")
            {
                // If the user canceled the sign in, redirect back to the home page
                notification.Response.Redirect("/");
            }
            else
            {
                notification.Response.Redirect("/Home/Error?message=" + notification.Exception.Message);
            }

            return Task.FromResult(0);
        }

        private Task OnSecurityTokenValidated(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            // If you wanted to keep some local state in the app (like a db of signed up users),
            // you could use this notification to create the user record if it does not already
            // exist.

            return Task.FromResult(0);
        }

        private Task OnRedirectToIdentityProvider(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            // If you wanted to keep some local state in the app (like a db of signed up users),
            // you could use this notification to create the user record if it does not already
            // exist.

            notification.ProtocolMessage.SetParameter("Custom", "Yoel");


            return Task.FromResult(0);
        }
        private OpenIdConnectAuthenticationOptions CreateOptionsFromPolicy(string policy)
        {
            return new OpenIdConnectAuthenticationOptions
            {

                // For each policy, give OWIN the policy-specific metadata address, and
                // set the authentication type to the id of the policy
                MetadataAddress = String.Format(aadInstance, tenant, policy),
                AuthenticationType = policy,

                // These are standard OpenID Connect parameters, with values pulled from web.config
                ClientId = clientId,
                ClientSecret = secretId,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = AuthenticationFailed,
                    SecurityTokenValidated = OnSecurityTokenValidated,
                    //RedirectToIdentityProvider = OnRedirectToIdentityProvider
                    RedirectToIdentityProvider = Callback,
                    SecurityTokenReceived = SecurityCallback,
                    AuthorizationCodeReceived = AuthorizationCodeCallback,
                    MessageReceived = MessageCallback

                },

                Scope = "openid offline_access", //%20offline_access

                // FIFA: when the web app also need tokens for calling a web API, use code+id_token.
                // Otherwise use the default id_token 
                ResponseType = "id_token",

                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    // FIFA: allowing access to the token itself using ClaimsPrincipal.Current.Identity.BootstrapContext
                    SaveSigninToken = true
                },
            };
        }

        private async Task Callback(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> config)
        {
            var i = config;
        }

        private async Task SecurityCallback(SecurityTokenReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> config)
        {
            var i = config;
        }

        private async Task AuthorizationCodeCallback(AuthorizationCodeReceivedNotification config)
        {
            var i = config;
        }

        private async Task MessageCallback(MessageReceivedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> config)
        {
            var i = config;
        }
    }
}
