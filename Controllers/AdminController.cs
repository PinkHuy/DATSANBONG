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
            // ── 1. Tổng số sân bóng ──
            // Count() đếm tất cả record trong bảng SanBong
            ViewBag.TongSan = db.SanBongs.Count();

            // ── 2. Tổng số khách hàng (người dùng) ──
            // Count() đếm tất cả record trong bảng NguoiDung
            ViewBag.TongKhach = db.NguoiDungs.Count();

            // ── 3. Tổng số đơn đặt sân ──
            // Count() đếm tất cả record trong bảng DatSan
            ViewBag.TongDon = db.DatSans.Count();

            // ── 4. Tổng doanh thu ──
            // Sum() tính tổng cột TongTien
            // Dùng try-catch vì: nếu bảng DatSan rỗng hoặc TongTien toàn null
            //   → Sum() sẽ trả về null → cần xử lý để tránh crash
            try
            {
                // Sum(d => d.TongTien) trả về decimal? (nullable)
                // Toán tử ?? (null-coalescing): nếu kết quả null thì gán = 0
                ViewBag.DoanhThu = db.DatSans.Sum(d => d.TongTien) ?? 0;
            }
            catch
            {
                // Phòng trường hợp lỗi bất ngờ (ví dụ: mất kết nối DB)
                ViewBag.DoanhThu = 0;
            }

            // ── 5. Lấy 5 đơn đặt sân mới nhất ──
            // OrderByDescending: sắp xếp GIẢM DẦN theo NgayDat (mới nhất lên đầu)
            // Take(5): chỉ lấy 5 bản ghi đầu tiên
            // ToList(): chuyển kết quả thành List để View dùng foreach
            var donMoiNhat = db.DatSans
                .OrderByDescending(d => d.NgayDat) // Sắp xếp: ngày mới nhất trước
                .Take(5)                           // Chỉ lấy 5 dòng
                .ToList();                         // Chuyển sang List<DatSan>

            ViewBag.DonMoiNhat = donMoiNhat;

            return View();
        }

        // GET: Admin
        // Hiển thị danh sách sân bóng
        public ActionResult Index()
        {
            var sanbongs = db.SanBongs.ToList();
            return View(sanbongs);
        }

        // GET: Admin/Create
        // Trả về form thêm mới
        public ActionResult Create()
        {
            // Truyền danh sách loại sân sang View để làm DropdownList
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai");
            return View();
        }

        // POST: Admin/Create
        // Xử lý dữ liệu thêm mới từ form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanBong sanBong, HttpPostedFileBase fileAnh)
        {
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

                // Thêm sân bóng vào DB
                db.SanBongs.InsertOnSubmit(sanBong);
                db.SubmitChanges();
                return RedirectToAction("Index");
            }

            // Nếu form không hợp lệ, tải lại danh sách loại sân
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);
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
            // Truyền danh sách loại sân và chọn sẵn loại hiện tại
            ViewBag.MaLoai = new SelectList(db.LoaiSans.ToList(), "MaLoai", "TenLoai", sanBong.MaLoai);
            return View(sanBong);
        }

        // POST: Admin/Edit/5
        // Xử lý lưu thay đổi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(SanBong sanBongEdit, HttpPostedFileBase fileAnh)
        {
            if (ModelState.IsValid)
            {
                // Tìm sân bóng trong cơ sở dữ liệu
                var sanBongDB = db.SanBongs.SingleOrDefault(s => s.MaSan == sanBongEdit.MaSan);
                if (sanBongDB != null)
                {
                    // Cập nhật các trường thông tin
                    sanBongDB.TenSan = sanBongEdit.TenSan;
                    sanBongDB.MaLoai = sanBongEdit.MaLoai;
                    sanBongDB.GiaTheoGio = sanBongEdit.GiaTheoGio;
                    sanBongDB.TrangThai = sanBongEdit.TrangThai;

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
            return View(sanBongEdit);
        }

        // GET: Admin/Delete/5
        // Xóa sân bóng dựa vào id
        public ActionResult Delete(int id)
        {
            var sanBong = db.SanBongs.SingleOrDefault(s => s.MaSan == id);
            if (sanBong != null)
            {
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
                don.TrangThai = "Đã hủy";
                db.SubmitChanges();
                TempData["ThanhCong"] = "Đã hủy đơn đặt sân thành công!";
            }
            return RedirectToAction("QuanLyDatSan");
        }

        private readonly DATSANBONG.Data.QuanLySanBongDb _dbSB = new DATSANBONG.Data.QuanLySanBongDb();

        // ============================================================
        // GET: Admin/QuanLyKhachHang
        // Quản lý danh sách khách hàng (không bao gồm Admin)
        // ============================================================
        public ActionResult QuanLyKhachHang()
        {
            var users = _dbSB.LayDanhSachNguoiDung()
                .Where(u => u.VaiTro != "Admin")
                .ToList();

            return View(users);
        }

        // ============================================================
        // GET: Admin/XoaKhachHang/5
        // Xóa khách hàng nếu không có đơn đặt sân đang hoạt động
        // ============================================================
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
            var datSans = db.DatSans.ToList();
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
            
            // Lấy tất cả các đơn đặt sân đã duyệt
            var bookings = db.DatSans
                .Where(d => d.TrangThai == "Đã duyệt" && d.NgayDat.Year == currentYear)
                .ToList();

            // Thống kê doanh thu theo 12 tháng
            var monthlyRevenue = Enumerable.Range(1, 12).Select(month => new {
                Month = "Tháng " + month,
                Revenue = bookings.Where(b => b.NgayDat.Month == month).Sum(b => b.TongTien) ?? 0
            }).ToList();

            // Thống kê doanh thu theo từng sân bóng
            var fieldRevenue = db.SanBongs.ToList().Select(s => new {
                FieldName = s.TenSan,
                Revenue = db.DatSans
                    .Where(d => d.MaSan == s.MaSan && d.TrangThai == "Đã duyệt")
                    .Sum(d => d.TongTien) ?? 0
            }).ToList();

            return Json(new { monthly = monthlyRevenue, fields = fieldRevenue }, JsonRequestBehavior.AllowGet);
        }
    }
}
