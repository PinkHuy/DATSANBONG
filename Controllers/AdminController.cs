using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    [AdminAuth]
    public class AdminController : Controller
    {
        DataClasses1DataContext db = new DataClasses1DataContext("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=QuanLySanBong_MVC;Integrated Security=True;MultipleActiveResultSets=True");

        // ============================================================
        // GET: Admin/Dashboard
        // Trang tổng quan dành cho Admin — hiển thị thống kê nhanh
        // ============================================================
        public ActionResult Dashboard()
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            bool isOwner = (role == "Owner" || role == "ChuSan" || role == "Chủ sân");

            if (isOwner && currentUserId.HasValue)
            {
                ViewBag.TongSan = db.SanBongs.Count(s => s.MaChuSan == currentUserId.Value);

                ViewBag.TongKhach = db.DatSans
                    .Where(d => d.SanBong.MaChuSan == currentUserId.Value && d.MaND != null)
                    .Select(d => d.MaND)
                    .Distinct()
                    .Count();

                ViewBag.TongDon = db.DatSans.Count(d => d.SanBong.MaChuSan == currentUserId.Value);

                try
                {
                    ViewBag.DoanhThu = db.DatSans
                        .Where(d => d.SanBong.MaChuSan == currentUserId.Value && d.TrangThai == "Đã duyệt")
                        .Sum(d => d.TongTien) ?? 0;
                }
                catch { ViewBag.DoanhThu = 0; }

                ViewBag.DonMoiNhat = db.DatSans
                    .Where(d => d.SanBong.MaChuSan == currentUserId.Value)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(5)
                    .ToList();
            }
            else
            {
                ViewBag.TongSan = db.SanBongs.Count();
                ViewBag.TongKhach = db.NguoiDungs.Count(u => u.VaiTro != "Admin");
                ViewBag.TongDon = db.DatSans.Count();

                try { ViewBag.DoanhThu = db.DatSans.Sum(d => d.TongTien) ?? 0; }
                catch { ViewBag.DoanhThu = 0; }

                ViewBag.DonMoiNhat = db.DatSans
                    .OrderByDescending(d => d.NgayDat)
                    .Take(5)
                    .ToList();

                // Tổng chủ sân (chỉ hiển thị khi là Admin)
                ViewBag.TongChuSan = _dbSB.LayDanhSachNguoiDungTheoVaiTro("Chủ sân").Count;
            }

            return View();
        }

        // ============================================================
        // GET: Admin/Index
        // Hiển thị danh sách sân bóng
        // ============================================================
        public ActionResult Index()
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            List<SanBong> sanbongs;
            if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && currentUserId.HasValue)
                sanbongs = db.SanBongs.Where(s => s.MaChuSan == currentUserId.Value).ToList();
            else
                sanbongs = db.SanBongs.ToList();

            return View(sanbongs);
        }

        // ============================================================
        // GET: Admin/Create
        // ============================================================
        public ActionResult Create()
        {
            string role = Session["VaiTro"]?.ToString();
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai");

            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan" || u.VaiTro == "Chủ sân").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen");
            }
            return View();
        }

        // ============================================================
        // POST: Admin/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanBong sanBong, HttpPostedFileBase fileAnh)
        {
            string role = Session["VaiTro"]?.ToString();
            if (ModelState.IsValid)
            {
                if (fileAnh != null && fileAnh.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(fileAnh.FileName);
                    var folderPath = Server.MapPath("~/Content/Images/SanBong");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                    fileAnh.SaveAs(Path.Combine(folderPath, fileName));
                    sanBong.HinhAnh = fileName;
                }

                if (role == "Owner" || role == "ChuSan" || role == "Chủ sân")
                    sanBong.MaChuSan = Convert.ToInt32(Session["MaND"]);

                db.SanBongs.InsertOnSubmit(sanBong);
                db.SubmitChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan" || u.VaiTro == "Chủ sân").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBong.MaChuSan);
            }
            return View(sanBong);
        }

        // ============================================================
        // GET: Admin/Edit/5
        // ============================================================
        public ActionResult Edit(int id)
        {
            var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == id);
            if (sanBong == null) return HttpNotFound();

            string role = Session["VaiTro"]?.ToString();
            if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
            {
                TempData["LoiThongBao"] = "Bạn không có quyền chỉnh sửa sân bóng này.";
                return RedirectToAction("Index");
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan" || u.VaiTro == "Chủ sân").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBong.MaChuSan);
            }
            return View(sanBong);
        }

        // ============================================================
        // POST: Admin/Edit/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SanBong sanBongEdit, HttpPostedFileBase fileAnh)
        {
            string role = Session["VaiTro"]?.ToString();
            if (ModelState.IsValid)
            {
                var sanBongDB = db.SanBongs.SingleOrDefault(s => s.MaSan == sanBongEdit.MaSan);
                if (sanBongDB != null)
                {
                    if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && sanBongDB.MaChuSan != Convert.ToInt32(Session["MaND"]))
                    {
                        TempData["LoiThongBao"] = "Bạn không có quyền chỉnh sửa sân bóng này.";
                        return RedirectToAction("Index");
                    }

                    sanBongDB.TenSan = sanBongEdit.TenSan;
                    sanBongDB.MaLoai = sanBongEdit.MaLoai;
                    sanBongDB.GiaTheoGio = sanBongEdit.GiaTheoGio;
                    sanBongDB.TrangThai = sanBongEdit.TrangThai;

                    if (role == "Admin")
                        sanBongDB.MaChuSan = sanBongEdit.MaChuSan;

                    if (fileAnh != null && fileAnh.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(fileAnh.FileName);
                        var folderPath = Server.MapPath("~/Content/Images/SanBong");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                        fileAnh.SaveAs(Path.Combine(folderPath, fileName));
                        sanBongDB.HinhAnh = fileName;
                    }

                    db.SubmitChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBongEdit.MaLoai);
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan" || u.VaiTro == "Chủ sân").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBongEdit.MaChuSan);
            }
            return View(sanBongEdit);
        }

        // ============================================================
        // GET: Admin/Delete/5
        // ============================================================
        public ActionResult Delete(int id)
        {
            var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == id);
            if (sanBong != null)
            {
                string role = Session["VaiTro"]?.ToString();
                if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền xóa sân bóng này.";
                    return RedirectToAction("Index");
                }
                db.SanBongs.DeleteOnSubmit(sanBong);
                db.SubmitChanges();
            }
            return RedirectToAction("Index");
        }

        // ============================================================
        // GET: Admin/QuanLyDatSan
        // Admin xem tất cả — Chủ sân chỉ thấy sân của mình
        // ============================================================
        public ActionResult QuanLyDatSan(string trangThai = "Chờ duyệt")
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null
                ? (int?)Convert.ToInt32(Session["MaND"]) : null;
            bool isChuSan = (role == "Owner" || role == "ChuSan" || role == "Chủ sân");

            IQueryable<DatSan> query = db.DatSans;

            // Chủ sân chỉ thấy đơn của sân mình quản lý
            if (isChuSan && currentUserId.HasValue)
                query = query.Where(d => d.SanBong.MaChuSan == currentUserId.Value);

            // Lọc theo trạng thái (mặc định "Chờ duyệt", "Tất cả" thì không lọc)
            if (trangThai != "Tất cả")
                query = query.Where(d => d.TrangThai == trangThai);

            // Đếm badge cho tab "Chờ duyệt"
            IQueryable<DatSan> choDuyetQuery = db.DatSans.Where(d => d.TrangThai == "Chờ duyệt");
            if (isChuSan && currentUserId.HasValue)
                choDuyetQuery = choDuyetQuery.Where(d => d.SanBong.MaChuSan == currentUserId.Value);

            ViewBag.SoChoDuyet = choDuyetQuery.Count();
            ViewBag.TrangThaiFilter = trangThai;
            ViewBag.TrangThaiChon = trangThai;

            return View(query.OrderByDescending(d => d.NgayDat).ToList());
        }

        // ============================================================
        // POST: Admin/DuyetDatSan
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DuyetDatSan(int maDatSan)
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null
                ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            var datSan = db.DatSans.FirstOrDefault(d => d.MaDatSan == maDatSan);
            if (datSan == null)
            {
                TempData["LoiThongBao"] = "Không tìm thấy đơn đặt sân!";
                return RedirectToAction("QuanLyDatSan");
            }

            // Bảo mật: chủ sân chỉ duyệt được sân của mình
            bool isChuSan = (role == "ChuSan" || role == "Owner" || role == "Chủ sân");
            if (isChuSan && currentUserId.HasValue)
            {
                var san = db.SanBongs.FirstOrDefault(s =>
                    s.MaSan == datSan.MaSan && s.MaChuSan == currentUserId.Value);
                if (san == null)
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền duyệt đơn này!";
                    return RedirectToAction("QuanLyDatSan");
                }
            }

            datSan.TrangThai = "Đã duyệt";
            db.SubmitChanges();

            TempData["ThanhCong"] = "Đã duyệt đơn đặt sân thành công!";
            return RedirectToAction("QuanLyDatSan");
        }

        // ============================================================
        // POST: Admin/TuChoiDatSan
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoiDatSan(int maDatSan)
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null
                ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            var datSan = db.DatSans.FirstOrDefault(d => d.MaDatSan == maDatSan);
            if (datSan == null)
            {
                TempData["LoiThongBao"] = "Không tìm thấy đơn đặt sân!";
                return RedirectToAction("QuanLyDatSan");
            }

            bool isChuSan = (role == "ChuSan" || role == "Owner" || role == "Chủ sân");
            if (isChuSan && currentUserId.HasValue)
            {
                var san = db.SanBongs.FirstOrDefault(s =>
                    s.MaSan == datSan.MaSan && s.MaChuSan == currentUserId.Value);
                if (san == null)
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền từ chối đơn này!";
                    return RedirectToAction("QuanLyDatSan");
                }
            }

            datSan.TrangThai = "Từ chối";
            db.SubmitChanges();

            TempData["ThanhCong"] = "Đã từ chối đơn đặt sân.";
            return RedirectToAction("QuanLyDatSan");
        }

        // ============================================================
        // GET: Admin/DuyetDon/5  (giữ lại tương thích GET cũ)
        // ============================================================
        public ActionResult DuyetDon(int id)
        {
            var don = db.DatSans.SingleOrDefault(d => d.MaDatSan == id);
            if (don != null)
            {
                string role = Session["VaiTro"]?.ToString();
                if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") &&
                    don.SanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền phê duyệt đơn đặt của sân này.";
                    return RedirectToAction("QuanLyDatSan");
                }
                don.TrangThai = "Đã duyệt";
                db.SubmitChanges();
                TempData["ThanhCong"] = "Phê duyệt đơn đặt sân thành công!";
            }
            return RedirectToAction("QuanLyDatSan");
        }

        // ============================================================
        // GET: Admin/HuyDon/5
        // ============================================================
        public ActionResult HuyDon(int id)
        {
            var don = db.DatSans.SingleOrDefault(d => d.MaDatSan == id);
            if (don != null)
            {
                string role = Session["VaiTro"]?.ToString();
                if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") &&
                    don.SanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền hủy đơn đặt của sân này.";
                    return RedirectToAction("QuanLyDatSan");
                }
                don.TrangThai = "Đã hủy";
                db.SubmitChanges();
                TempData["ThanhCong"] = "Đã hủy đơn đặt sân thành công!";
            }
            return RedirectToAction("QuanLyDatSan");
        }

        private readonly DATSANBONG.Data.QuanLySanBongDb _dbSB = new DATSANBONG.Data.QuanLySanBongDb();

        // ============================================================
        // GET: Admin/QuanLyKhachHang (chỉ Admin tổng)
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult QuanLyKhachHang()
        {
            var users = _dbSB.LayDanhSachNguoiDung()
                .Where(u => u.VaiTro != "Admin")
                .ToList();
            return View(users);
        }

        // ============================================================
        // GET: Admin/XoaKhachHang/5 (chỉ Admin tổng)
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult XoaKhachHang(int id)
        {
            var datSans = _dbSB.LayLichSuDatSanTheoNguoiDung(id)
                .Where(d => d.TrangThai != "Đã hủy")
                .ToList();

            if (datSans.Any())
                TempData["LoiThongBao"] = "Không thể xóa vì khách hàng còn đơn đặt sân đang hoạt động.";
            else
            {
                _dbSB.XoaNguoiDung(id);
                TempData["ThanhCong"] = "Xóa khách hàng thành công!";
            }
            return RedirectToAction("QuanLyKhachHang");
        }

        // ============================================================
        // GET: Admin/LichDatSan
        // ============================================================
        public ActionResult LichDatSan()
        {
            return View();
        }

        // ============================================================
        // GET: Admin/GetDatSanJson  (JSON cho FullCalendar)
        // ============================================================
        public JsonResult GetDatSanJson()
        {
            var query = db.DatSans.AsQueryable();

            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && currentUserId.HasValue)
                query = query.Where(d => d.SanBong.MaChuSan == currentUserId.Value);

            var events = query.ToList().Select(d => new {
                id = d.MaDatSan,
                title = (d.SanBong != null ? d.SanBong.TenSan : "Sân N/A")
                      + " - "
                      + (d.NguoiDung != null ? d.NguoiDung.HoTen : "Khách N/A"),
                start = (d.NgayDat.Date + d.GioBatDau).ToString("yyyy-MM-ddTHH:mm:ss"),
                end = (d.NgayDat.Date + d.GioKetThuc).ToString("yyyy-MM-ddTHH:mm:ss"),
                color = d.TrangThai == "Đã duyệt" ? "#28a745"
                      : d.TrangThai == "Chờ duyệt" ? "#ffc107" : "#dc3545",
                textColor = d.TrangThai == "Chờ duyệt" ? "#212529" : "#ffffff",
                extendedProps = new
                {
                    status = d.TrangThai,
                    phone = d.NguoiDung != null ? d.NguoiDung.SoDienThoai : "",
                    price = d.TongTien.HasValue ? d.TongTien.Value.ToString("N0") + " VNĐ" : "0 VNĐ"
                }
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        // ============================================================
        // GET: Admin/ThongKe
        // ============================================================
        public ActionResult ThongKe()
        {
            return View();
        }

        // ============================================================
        // GET: Admin/GetDoanhThuJson  (JSON cho Chart.js)
        // ============================================================
        public JsonResult GetDoanhThuJson()
        {
            var currentYear = DateTime.Now.Year;
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;
            bool isOwner = (role == "Owner" || role == "ChuSan" || role == "Chủ sân") && currentUserId.HasValue;

            var bookingsQuery = db.DatSans
                .Where(d => d.TrangThai == "Đã duyệt" && d.NgayDat.Year == currentYear);
            if (isOwner)
                bookingsQuery = bookingsQuery.Where(d => d.SanBong.MaChuSan == currentUserId.Value);
            var bookings = bookingsQuery.ToList();

            var monthlyRevenue = Enumerable.Range(1, 12).Select(month => new {
                Month = "Tháng " + month,
                Revenue = bookings.Where(b => b.NgayDat.Month == month).Sum(b => b.TongTien) ?? 0
            }).ToList();

            var fieldsQuery = db.SanBongs.AsQueryable();
            if (isOwner) fieldsQuery = fieldsQuery.Where(s => s.MaChuSan == currentUserId.Value);
            var fields = fieldsQuery.ToList();

            var fieldRevenue = fields.Select(s => new {
                FieldName = s.TenSan,
                Revenue = db.DatSans
                    .Where(d => d.MaSan == s.MaSan && d.TrangThai == "Đã duyệt")
                    .Sum(d => d.TongTien) ?? 0
            }).ToList();

            return Json(new { monthly = monthlyRevenue, fields = fieldRevenue }, JsonRequestBehavior.AllowGet);
        }

        // ============================================================
        // GET: Admin/QuanLyChuSan  (chỉ Admin tổng)
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult QuanLyChuSan()
        {
            var chuSans = _dbSB.LayDanhSachNguoiDungTheoVaiTro("Chủ sân");
            ViewBag.DanhSachSan = db.SanBongs.ToList();
            ViewBag.SanCuaChuSan = _dbSB.LaySanCuaTungChuSan();
            return View(chuSans);
        }

        // ============================================================
        // POST: Admin/ThemChuSan
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuth(Roles = "Admin")]
        public ActionResult ThemChuSan(string HoTen, string TaiKhoan, string MatKhau, string SoDienThoai)
        {
            if (string.IsNullOrWhiteSpace(HoTen) || string.IsNullOrWhiteSpace(TaiKhoan) || string.IsNullOrWhiteSpace(MatKhau))
            {
                TempData["LoiThongBao"] = "Vui lòng nhập đầy đủ họ tên, tài khoản và mật khẩu.";
                return RedirectToAction("QuanLyChuSan");
            }

            if (_dbSB.TaiKhoanDaTonTai(TaiKhoan.Trim()))
            {
                TempData["LoiThongBao"] = $"Tài khoản \"{TaiKhoan.Trim()}\" đã tồn tại trong hệ thống.";
                return RedirectToAction("QuanLyChuSan");
            }

            var nd = new DATSANBONG.Models.NguoiDung
            {
                HoTen = HoTen.Trim(),
                TaiKhoan = TaiKhoan.Trim(),
                MatKhau = DATSANBONG.Data.QuanLySanBongDb.HashPassword(MatKhau),
                SoDienThoai = string.IsNullOrWhiteSpace(SoDienThoai) ? null : SoDienThoai.Trim(),
                VaiTro = "Chủ sân"
            };
            _dbSB.ThemNguoiDung(nd);
            TempData["ThanhCong"] = $"Đã thêm chủ sân \"{nd.HoTen}\" thành công.";
            return RedirectToAction("QuanLyChuSan");
        }

        // ============================================================
        // POST: Admin/PhanCongSan
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuth(Roles = "Admin")]
        public ActionResult PhanCongSan(int MaND, int MaSan)
        {
            _dbSB.PhanCongSanChoChuSan(MaND, MaSan);
            TempData["ThanhCong"] = "Đã phân công sân thành công.";
            return RedirectToAction("QuanLyChuSan");
        }

        // ============================================================
        // GET: Admin/DoiVaiTro?id=&vaiTro=
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult DoiVaiTro(int id, string vaiTro)
        {
            _dbSB.CapNhatVaiTro(id, vaiTro);
            if (vaiTro == "Khách hàng")
            {
                _dbSB.GoChuSanKhoiTatCaSan(id);
                TempData["ThanhCong"] = "Đã thu hồi quyền Chủ sân thành công.";
            }
            else
            {
                TempData["ThanhCong"] = $"Đã đổi vai trò thành \"{vaiTro}\" thành công.";
            }
            return RedirectToAction("QuanLyChuSan");
        }

        // ============================================================
        // GET: Admin/XoaChuSan/5
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult XoaChuSan(int id)
        {
            _dbSB.GoChuSanKhoiTatCaSan(id);
            _dbSB.XoaNguoiDung(id);
            TempData["ThanhCong"] = "Đã xóa chủ sân thành công.";
            return RedirectToAction("QuanLyChuSan");
        }

        // ============================================================
        // GET: Admin/CauHinhGia
        // ============================================================
        public ActionResult CauHinhGia()
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            List<SanBong> sanbongs;
            if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") && currentUserId.HasValue)
                sanbongs = db.SanBongs.Where(s => s.MaChuSan == currentUserId.Value).ToList();
            else
                sanbongs = db.SanBongs.ToList();

            return View(sanbongs);
        }

        // ============================================================
        // POST: Admin/CapNhatGia  (AJAX)
        // ============================================================
        [HttpPost]
        public JsonResult CapNhatGia(int maSan, decimal giaMoi)
        {
            try
            {
                var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == maSan);
                if (sanBong == null)
                    return Json(new { success = false, message = "Không tìm thấy sân bóng." });

                string role = Session["VaiTro"]?.ToString();
                if ((role == "Owner" || role == "ChuSan" || role == "Chủ sân") &&
                    sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
                    return Json(new { success = false, message = "Bạn không có quyền cấu hình sân bóng này." });

                if (giaMoi < 0)
                    return Json(new { success = false, message = "Giá thuê phải lớn hơn hoặc bằng 0." });

                sanBong.GiaTheoGio = giaMoi;
                db.SubmitChanges();
                return Json(new { success = true, message = "Cập nhật giá thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
