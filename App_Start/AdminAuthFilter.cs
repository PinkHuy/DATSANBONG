using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DATSANBONG
{
    public class AdminAuthFilter : ActionFilterAttribute
    {
        public string Roles { get; set; }

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
                return;
            }

            string userRole = session["VaiTro"]?.ToString();
            bool isAuthorized = false;

            if (string.IsNullOrEmpty(Roles))
            {
                // Mặc định: cho phép Admin và Owner (hoặc ChuSan) truy cập
                isAuthorized = (userRole == "Admin" || userRole == "Owner" || userRole == "ChuSan");
            }
            else
            {
                // Phân tích danh sách vai trò
                var allowedRoles = Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(r => r.Trim())
                                        .ToList();

                isAuthorized = allowedRoles.Contains(userRole) ||
                               (userRole == "ChuSan" && allowedRoles.Contains("Owner")) ||
                               (userRole == "Owner" && allowedRoles.Contains("ChuSan"));
            }

            // 2. Nếu không đủ quyền -> redirect về /Home/Index kèm thông báo lỗi
            if (!isAuthorized)
            {
                filterContext.Controller.TempData["LoiThongBao"] = "Bạn không có quyền truy cập trang này.";
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Home",
                        action = "Index"
                    })
                );
                return;
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
