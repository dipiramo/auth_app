using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace Microsoft.Mdp.Identity.Demo.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.UserProfileId = "To get user profile ID you must login first!";
            var data = HttpContext.GetOwinContext();

            var state = Request.Form["state"];
            var code = Request.Form["code"];

            if (ClaimsPrincipal.Current != null)
            {
                foreach (var claim in ClaimsPrincipal.Current.Claims)
                {
                    if (claim.Type == "UserProfileId")
                    {
                        ViewBag.UserProfileId = new HtmlString("<span id='fanProfileID'>" + claim.Value + "</span>");
                        break;
                    }
                }
            }
            return View();
        }

        public ActionResult Token()
        {
            ViewBag.Token = "JWT token not found";

            if (ClaimsPrincipal.Current != null && ClaimsPrincipal.Current.Identity != null)
            {
                var ci = (System.Security.Claims.ClaimsIdentity)ClaimsPrincipal.Current.Identity;

                if (ci.BootstrapContext != null)
                    ViewBag.Token = ((System.IdentityModel.Tokens.BootstrapContext)ci.BootstrapContext).Token;
            }

            return View();
        }

        // You can use the PolicyAuthorize decorator to execute a certain policy if the user is not already signed into the app.
        [Authorize]
        public ActionResult Claims()
        {
            Claim displayName = ClaimsPrincipal.Current.FindFirst(ClaimsPrincipal.Current.Identities.First().NameClaimType);
            ViewBag.DisplayName = displayName != null ? displayName.Value : string.Empty;
            return View();
        }

        public ActionResult Error(string message)
        {
            ViewBag.Message = message;

            return View("Error");
        }
    }
}