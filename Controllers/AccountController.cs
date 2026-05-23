using System.Web.Mvc;
using DATSANBONG.Data;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLySanBongDb _db = new QuanLySanBongDb();

        // ─── ĐĂNG NHẬP ────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DangNhap(string returnUrl)
        {
            // Nếu đã đăng nhập → về trang chủ
            if (Session["MaND"] != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            string matKhauHash = QuanLySanBongDb.HashPassword(model.MatKhau);
            NguoiDung nd = _db.DangNhap(model.TaiKhoan, matKhauHash);

            if (nd == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Lưu session
            Session["MaND"]   = nd.MaND;
            Session["HoTen"]  = nd.HoTen;
            Session["VaiTro"] = nd.VaiTro;

            // Điều hướng theo vai trò hoặc returnUrl
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return nd.VaiTro == "Admin"
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Home");
        }

        // ─── ĐĂNG KÝ ──────────────────────────────────────────────────

        [HttpGet]
        public ActionResult DangKy()
        {
            if (Session["MaND"] != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra tài khoản đã tồn tại
            if (_db.TaiKhoanDaTonTai(model.TaiKhoan))
            {
                ModelState.AddModelError("TaiKhoan", "Tài khoản này đã được sử dụng. Vui lòng chọn tài khoản khác.");
                return View(model);
            }

            var nguoiDung = new NguoiDung
            {
                HoTen       = model.HoTen.Trim(),
                TaiKhoan    = model.TaiKhoan.Trim(),
                MatKhau     = QuanLySanBongDb.HashPassword(model.MatKhau),
                SoDienThoai = model.SoDienThoai?.Trim(),
                VaiTro      = "Khách hàng"
            };

            int maND = _db.ThemNguoiDung(nguoiDung);

            // Tự động đăng nhập sau khi đăng ký
            Session["MaND"]   = maND;
            Session["HoTen"]  = nguoiDung.HoTen;
            Session["VaiTro"] = nguoiDung.VaiTro;

            TempData["ThanhCong"] = "Đăng ký thành công! Chào mừng bạn đến với DatSanBong.";
            return RedirectToAction("Index", "Home");
        }

        // ─── ĐĂNG XUẤT (LOGOUT) ────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        // GET version cho link đăng xuất (fallback)
        [HttpGet]
        public ActionResult LogoutGet()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }
    }
}
