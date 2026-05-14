using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
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
                    // Đường dẫn lưu file
                    var path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
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
                        var path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
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
    }
}
