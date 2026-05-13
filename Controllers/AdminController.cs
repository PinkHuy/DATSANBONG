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
        DataClasses1DataContext db = new DataClasses1DataContext();

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
