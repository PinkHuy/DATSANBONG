using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DATSANBONG
{
    public class AdminAuthFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;

            // 1. Kiểm tra nếu Session["MaND"] == null -> redirect về /Account/DangNhap?returnUrl=...
            if (session == null || session["MaND"] == null)
            {
                string returnUrl = filterContext.HttpContext.Request.RawUrl;
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Account",
                        action = "DangNhap",
                        returnUrl = returnUrl
                    })
                );
            }
            // 2. Kiểm tra nếu Session["VaiTro"] != "Admin" -> redirect về /Home/Index kèm thông báo lỗi
            else if (session["VaiTro"]?.ToString() != "Admin")
            {
                filterContext.Controller.TempData["LoiThongBao"] = "Bạn không có quyền truy cập trang này.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Home",
                        action = "Index"
                    })
                );
            }

            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// Alias attribute class so that we can use [AdminAuth] on controllers/actions
    /// </summary>
    public class AdminAuthAttribute : AdminAuthFilter
    {
    }
}
