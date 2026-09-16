using System.Web.Mvc;

namespace MovieTicketMVC
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(
            GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}