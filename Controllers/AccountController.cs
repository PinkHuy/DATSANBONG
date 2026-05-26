using System.Web.Mvc;
using DATSANBONG.Data;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    public class AccountController : Controller
    {
        private readonly QuanLySanBongDb _db = new QuanLySanBongDb();

        // ─── HELPER: Chuẩn hóa vai trò ───────────────────────────────
        private string ChuanHoaRole(string vaiTro)
        {
            switch (vaiTro?.Trim())
            {
                case "ChuSan":
                case "Chủ sân": return "Owner";
                case "KhachHang":
                case "Khách hàng": return "KhachHang";
                default: return vaiTro; // "Admin" giữ nguyên
            }
        }

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

            // Chuẩn hóa vai trò
            string role = ChuanHoaRole(nd.VaiTro);

            Session["MaND"] = nd.MaND;
            Session["HoTen"] = nd.HoTen;
            Session["VaiTro"] = role;
            Session["UserRole"] = role;

            if (role == "Owner")
                Session["MaChuSan"] = nd.MaND;

            // Điều hướng sau đăng nhập
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                // Chỉ redirect returnUrl nếu user có quyền Admin/Owner
                if (role == "Admin" || role == "Owner")
                    return Redirect(returnUrl);
            }

            if (role == "Admin" || role == "Owner")
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Index", "Home");
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
                HoTen = model.HoTen.Trim(),
                TaiKhoan = model.TaiKhoan.Trim(),
                MatKhau = QuanLySanBongDb.HashPassword(model.MatKhau),
                SoDienThoai = model.SoDienThoai?.Trim(),
                VaiTro = "Khách hàng"
            };

            int maND = _db.ThemNguoiDung(nguoiDung);

            // Chuẩn hóa role trước khi lưu session
            string role = ChuanHoaRole(nguoiDung.VaiTro); // → "KhachHang"

            Session["MaND"] = maND;
            Session["HoTen"] = nguoiDung.HoTen;
            Session["VaiTro"] = role;
            Session["UserRole"] = role;

            TempData["ThanhCong"] = "Đăng ký thành công! Chào mừng bạn đến với DatSanBong.";
            return RedirectToAction("Index", "Home");
        }

        // ─── ĐĂNG XUẤT ────────────────────────────────────────────────

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