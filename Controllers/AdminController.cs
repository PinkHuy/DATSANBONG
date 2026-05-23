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

            bool isOwner = (role == "Owner" || role == "ChuSan");

            if (isOwner && currentUserId.HasValue)
            {
                // ── 1. Tổng số sân bóng thuộc sở hữu của Chủ sân này ──
                ViewBag.TongSan = db.SanBongs.Count(s => s.MaChuSan == currentUserId.Value);

                // ── 2. Tổng số khách hàng đã từng đặt sân của Chủ sân này ──
                ViewBag.TongKhach = db.DatSans
                    .Where(d => d.SanBong.MaChuSan == currentUserId.Value && d.MaND != null)
                    .Select(d => d.MaND)
                    .Distinct()
                    .Count();

                // ── 3. Tổng số đơn đặt sân thuộc các sân của Chủ sân này ──
                ViewBag.TongDon = db.DatSans.Count(d => d.SanBong.MaChuSan == currentUserId.Value);

                // ── 4. Tổng doanh thu của các sân thuộc Chủ sân này ──
                try
                {
                    ViewBag.DoanhThu = db.DatSans
                        .Where(d => d.SanBong.MaChuSan == currentUserId.Value && d.TrangThai == "Đã duyệt")
                        .Sum(d => d.TongTien) ?? 0;
                }
                catch
                {
                    ViewBag.DoanhThu = 0;
                }

                // ── 5. Lấy 5 đơn đặt sân mới nhất thuộc quyền sở hữu ──
                var donMoiNhat = db.DatSans
                    .Where(d => d.SanBong.MaChuSan == currentUserId.Value)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(5)
                    .ToList();

                ViewBag.DonMoiNhat = donMoiNhat;
            }
            else
            {
                // ── 1. Tổng số sân bóng toàn hệ thống ──
                ViewBag.TongSan = db.SanBongs.Count();

                // ── 2. Tổng số khách hàng toàn hệ thống ──
                ViewBag.TongKhach = db.NguoiDungs.Count(u => u.VaiTro != "Admin");

                // ── 3. Tổng số đơn đặt sân toàn hệ thống ──
                ViewBag.TongDon = db.DatSans.Count();

                // ── 4. Tổng doanh thu toàn hệ thống ──
                try
                {
                    ViewBag.DoanhThu = db.DatSans.Sum(d => d.TongTien) ?? 0;
                }
                catch
                {
                    ViewBag.DoanhThu = 0;
                }

                // ── 5. Lấy 5 đơn đặt sân mới nhất toàn hệ thống ──
                var donMoiNhat = db.DatSans
                    .OrderByDescending(d => d.NgayDat)
                    .Take(5)
                    .ToList();

                ViewBag.DonMoiNhat = donMoiNhat;
            }

            return View();
        }

        // GET: Admin
        // Hiển thị danh sách sân bóng
        public ActionResult Index()
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            List<SanBong> sanbongs;
            if ((role == "Owner" || role == "ChuSan") && currentUserId.HasValue)
            {
                sanbongs = db.SanBongs.Where(s => s.MaChuSan == currentUserId.Value).ToList();
            }
            else
            {
                sanbongs = db.SanBongs.ToList();
            }

            return View(sanbongs);
        }

        // GET: Admin/Create
        // Trả về form thêm mới
        public ActionResult Create()
        {
            string role = Session["VaiTro"]?.ToString();
            // Truyền danh sách loại sân sang View để làm DropdownList
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai");

            // Nếu là Admin, truyền thêm danh sách chủ sân
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen");
            }
            return View();
        }

        // POST: Admin/Create
        // Xử lý dữ liệu thêm mới từ form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanBong sanBong, HttpPostedFileBase fileAnh)
        {
            string role = Session["VaiTro"]?.ToString();
            // Kiểm tra validation của form
            if (ModelState.IsValid)
            {
                // Xử lý upload file ảnh
                if (fileAnh != null && fileAnh.ContentLength > 0)
                {
                    // Lấy tên file
                    var fileName = Path.GetFileName(fileAnh.FileName);
                    // Đường dẫn thư mục lưu file
                    var folderPath = Server.MapPath("~/Content/Images/SanBong");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    // Đường dẫn lưu file
                    var path = Path.Combine(folderPath, fileName);
                    // Lưu file vào server
                    fileAnh.SaveAs(path);
                    // Lưu tên file vào database
                    sanBong.HinhAnh = fileName;
                }

                // Gán chủ sân bóng
                if (role == "Owner" || role == "ChuSan")
                {
                    sanBong.MaChuSan = Convert.ToInt32(Session["MaND"]);
                }

                // Thêm sân bóng vào DB
                db.SanBongs.InsertOnSubmit(sanBong);
                db.SubmitChanges();
                return RedirectToAction("Index");
            }

            // Nếu form không hợp lệ, tải lại danh sách loại sân
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBong.MaChuSan);
            }
            return View(sanBong);
        }

        // GET: Admin/Edit/5
        // Lấy dữ liệu của 1 sân bóng để hiển thị lên form chỉnh sửa
        public ActionResult Edit(int id)
        {
            var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == id);
            if (sanBong == null)
            {
                return HttpNotFound();
            }

            string role = Session["VaiTro"]?.ToString();
            // Kiểm soát bảo mật: không được sửa sân của chủ khác
            if ((role == "Owner" || role == "ChuSan") && sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
            {
                TempData["LoiThongBao"] = "Bạn không có quyền chỉnh sửa sân bóng này.";
                return RedirectToAction("Index");
            }

            // Truyền danh sách loại sân và chọn sẵn loại hiện tại
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);

            // Nếu là Admin, truyền danh sách chủ sân để đổi nếu cần
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBong.MaChuSan);
            }

            return View(sanBong);
        }

        // POST: Admin/Edit/5
        // Xử lý lưu thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SanBong sanBongEdit, HttpPostedFileBase fileAnh)
        {
            string role = Session["VaiTro"]?.ToString();

            if (ModelState.IsValid)
            {
                // Tìm sân bóng trong cơ sở dữ liệu
                var sanBongDB = db.SanBongs.SingleOrDefault(s => s.MaSan == sanBongEdit.MaSan);
                if (sanBongDB != null)
                {
                    // Kiểm soát bảo mật
                    if ((role == "Owner" || role == "ChuSan") && sanBongDB.MaChuSan != Convert.ToInt32(Session["MaND"]))
                    {
                        TempData["LoiThongBao"] = "Bạn không có quyền chỉnh sửa sân bóng này.";
                        return RedirectToAction("Index");
                    }

                    // Cập nhật các trường thông tin
                    sanBongDB.TenSan = sanBongEdit.TenSan;
                    sanBongDB.MaLoai = sanBongEdit.MaLoai;
                    sanBongDB.GiaTheoGio = sanBongEdit.GiaTheoGio;
                    sanBongDB.TrangThai = sanBongEdit.TrangThai;

                    // Nếu là Admin, cho phép chuyển đổi chủ sở hữu
                    if (role == "Admin")
                    {
                        sanBongDB.MaChuSan = sanBongEdit.MaChuSan;
                    }

                    // Nếu có upload ảnh mới thì thay thế
                    if (fileAnh != null && fileAnh.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(fileAnh.FileName);
                        var folderPath = Server.MapPath("~/Content/Images/SanBong");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        var path = Path.Combine(folderPath, fileName);
                        fileAnh.SaveAs(path);
                        sanBongDB.HinhAnh = fileName;
                    }

                    db.SubmitChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBongEdit.MaLoai);
            if (role == "Admin")
            {
                var owners = db.NguoiDungs.Where(u => u.VaiTro == "Owner" || u.VaiTro == "ChuSan").ToList();
                ViewBag.MaChuSan = new SelectList(owners, "MaND", "HoTen", sanBongEdit.MaChuSan);
            }
            return View(sanBongEdit);
        }

        // GET: Admin/Delete/5
        // Xóa sân bóng dựa vào id
        public ActionResult Delete(int id)
        {
            var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == id);
            if (sanBong != null)
            {
                string role = Session["VaiTro"]?.ToString();
                // Kiểm soát bảo mật
                if ((role == "Owner" || role == "ChuSan") && sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
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
        // Quản lý tất cả đơn đặt sân của khách hàng
        // ============================================================
        public ActionResult QuanLyDatSan(string trangThai)
        {
            var query = db.DatSans.AsQueryable();

            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            // Nếu là Chủ sân, chỉ hiện đơn hàng của các sân thuộc về họ
            if ((role == "Owner" || role == "ChuSan") && currentUserId.HasValue)
            {
                query = query.Where(d => d.SanBong.MaChuSan == currentUserId.Value);
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai == trangThai);
            }

            var listDatSan = query.OrderByDescending(d => d.NgayDat).ToList();
            ViewBag.TrangThaiChon = trangThai;

            return View(listDatSan);
        }

        // ============================================================
        // GET: Admin/DuyetDon/5
        // Phê duyệt yêu cầu đặt sân
        // ============================================================
        public ActionResult DuyetDon(int id)
        {
            var don = db.DatSans.SingleOrDefault(d => d.MaDatSan == id);
            if (don != null)
            {
                string role = Session["VaiTro"]?.ToString();
                // Bảo mật chéo
                if ((role == "Owner" || role == "ChuSan") && don.SanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
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
        // Hủy đơn đặt sân
        // ============================================================
        public ActionResult HuyDon(int id)
        {
            var don = db.DatSans.SingleOrDefault(d => d.MaDatSan == id);
            if (don != null)
            {
                string role = Session["VaiTro"]?.ToString();
                // Bảo mật chéo
                if ((role == "Owner" || role == "ChuSan") && don.SanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
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
        // GET: Admin/QuanLyKhachHang
        // Quản lý danh sách khách hàng (chỉ dành riêng cho Admin tổng)
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
        // GET: Admin/XoaKhachHang/5
        // Xóa khách hàng (chỉ dành riêng cho Admin tổng)
        // ============================================================
        [AdminAuth(Roles = "Admin")]
        public ActionResult XoaKhachHang(int id)
        {
            // Kiểm tra xem khách hàng có đơn đặt sân nào chưa hủy không
            var datSans = _dbSB.LayLichSuDatSanTheoNguoiDung(id)
                .Where(d => d.TrangThai != "Đã hủy")
                .ToList();

            if (datSans.Any())
            {
                TempData["LoiThongBao"] = "Không thể xóa vì khách hàng còn đơn đặt sân đang hoạt động.";
            }
            else
            {
                _dbSB.XoaNguoiDung(id);
                TempData["ThanhCong"] = "Xóa khách hàng thành công!";
            }

            return RedirectToAction("QuanLyKhachHang");
        }

        // ============================================================
        // GET: Admin/LichDatSan
        // Xem lịch đặt sân dưới dạng FullCalendar ô vuông
        // ============================================================
        public ActionResult LichDatSan()
        {
            return View();
        }

        // ============================================================
        // GET: Admin/GetDatSanJson
        // Trả về danh sách đơn đặt sân dưới dạng JSON cho FullCalendar
        // ============================================================
        public JsonResult GetDatSanJson()
        {
            var query = db.DatSans.AsQueryable();

            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            // Nếu là chủ sân, lọc theo sân thuộc quyền sở hữu
            if ((role == "Owner" || role == "ChuSan") && currentUserId.HasValue)
            {
                query = query.Where(d => d.SanBong.MaChuSan == currentUserId.Value);
            }

            var datSans = query.ToList();
            var events = datSans.Select(d => new {
                id = d.MaDatSan,
                title = (d.SanBong != null ? d.SanBong.TenSan : "Sân N/A") + " - " + (d.NguoiDung != null ? d.NguoiDung.HoTen : "Khách N/A"),
                start = (d.NgayDat.Date + d.GioBatDau).ToString("yyyy-MM-ddTHH:mm:ss"),
                end = (d.NgayDat.Date + d.GioKetThuc).ToString("yyyy-MM-ddTHH:mm:ss"),
                color = d.TrangThai == "Đã duyệt" ? "#28a745" : (d.TrangThai == "Chờ duyệt" ? "#ffc107" : "#dc3545"),
                textColor = d.TrangThai == "Chờ duyệt" ? "#212529" : "#ffffff",
                extendedProps = new {
                    status = d.TrangThai,
                    phone = d.NguoiDung != null ? d.NguoiDung.SoDienThoai : "",
                    price = d.TongTien.HasValue ? d.TongTien.Value.ToString("N0") + " VNĐ" : "0 VNĐ"
                }
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        // ============================================================
        // GET: Admin/ThongKe
        // Giao diện vẽ biểu đồ thống kê doanh thu
        // ============================================================
        public ActionResult ThongKe()
        {
            return View();
        }

        // ============================================================
        // GET: Admin/GetDoanhThuJson
        // Trả về dữ liệu thống kê doanh thu dạng JSON cho Chart.js
        // ============================================================
        public JsonResult GetDoanhThuJson()
        {
            var currentYear = DateTime.Now.Year;

            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;
            bool isOwner = (role == "Owner" || role == "ChuSan") && currentUserId.HasValue;

            // Lấy tất cả các đơn đặt sân đã duyệt
            var bookingsQuery = db.DatSans.Where(d => d.TrangThai == "Đã duyệt" && d.NgayDat.Year == currentYear);
            if (isOwner)
            {
                bookingsQuery = bookingsQuery.Where(d => d.SanBong.MaChuSan == currentUserId.Value);
            }
            var bookings = bookingsQuery.ToList();

            // Thống kê doanh thu theo 12 tháng
            var monthlyRevenue = Enumerable.Range(1, 12).Select(month => new {
                Month = "Tháng " + month,
                Revenue = bookings.Where(b => b.NgayDat.Month == month).Sum(b => b.TongTien) ?? 0
            }).ToList();

            // Thống kê doanh thu theo từng sân bóng
            var fieldsQuery = db.SanBongs.AsQueryable();
            if (isOwner)
            {
                fieldsQuery = fieldsQuery.Where(s => s.MaChuSan == currentUserId.Value);
            }
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
        // GET: Admin/CauHinhGia
        // ============================================================
        public ActionResult CauHinhGia()
        {
            string role = Session["VaiTro"]?.ToString();
            int? currentUserId = Session["MaND"] != null ? (int?)Convert.ToInt32(Session["MaND"]) : null;

            List<SanBong> sanbongs;
            if ((role == "Owner" || role == "ChuSan") && currentUserId.HasValue)
            {
                sanbongs = db.SanBongs.Where(s => s.MaChuSan == currentUserId.Value).ToList();
            }
            else
            {
                sanbongs = db.SanBongs.ToList();
            }

            return View(sanbongs);
        }

        // ============================================================
        // POST: Admin/CapNhatGia
        // Cập nhật giá theo giờ nhanh qua AJAX
        // ============================================================
        [HttpPost]
        public JsonResult CapNhatGia(int maSan, decimal giaMoi)
        {
            try
            {
                var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == maSan);
                if (sanBong == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sân bóng." });
                }

                string role = Session["VaiTro"]?.ToString();
                // Kiểm soát bảo mật
                if ((role == "Owner" || role == "ChuSan") && sanBong.MaChuSan != Convert.ToInt32(Session["MaND"]))
                {
                    return Json(new { success = false, message = "Bạn không có quyền cấu hình sân bóng này." });
                }

                if (giaMoi < 0)
                {
                    return Json(new { success = false, message = "Giá thuê phải lớn hơn hoặc bằng 0." });
                }

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

