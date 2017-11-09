using System.Web;
using System.Web.Mvc;

namespace Microsoft.Mdp.Identity.Demo.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
