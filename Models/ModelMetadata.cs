using System;
using System.Web;
using System.Web.Mvc;

namespace DATSANBONG.Models
{
    /// <summary>
    /// Filter kiểm tra quyền truy cập trang Admin.
    /// Cho phép: Admin, Owner (đã chuẩn hóa từ ChuSan/Chủ sân)
    /// </summary>
    public class AdminAuthAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var session = httpContext.Session;
            if (session == null || session["MaND"] == null) return false;

            string role = session["VaiTro"]?.ToString() ?? "";

            // Nếu có chỉ định Roles cụ thể (vd: [AdminAuth(Roles = "Admin")])
            if (!string.IsNullOrEmpty(Roles))
            {
                var allowedRoles = Roles.Split(',');
                foreach (var r in allowedRoles)
                    if (role == r.Trim()) return true;
                return false;
            }

            // Mặc định: cho phép Admin và Owner
            // (ChuSan / Chủ sân đã được chuẩn hóa thành "Owner" trong AccountController)
            return role == "Admin" || role == "Owner";
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var session = filterContext.HttpContext.Session;

            if (session == null || session["MaND"] == null)
            {
                // Chưa đăng nhập → chuyển về trang đăng nhập, giữ lại returnUrl
                string returnUrl = filterContext.HttpContext.Request.RawUrl;
                filterContext.Result = new RedirectResult(
                    "/Account/DangNhap?returnUrl=" + HttpUtility.UrlEncode(returnUrl));
            }
            else
            {
                // Đã đăng nhập nhưng không đủ quyền → về trang chủ + thông báo
                filterContext.Controller.TempData["LoiThongBao"] =
                    "Bạn không có quyền truy cập trang này.";
                filterContext.Result = new RedirectResult("/Home/Index");
            }
        }
    }
}