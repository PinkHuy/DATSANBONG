using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATSANBONG.Data;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    public class SanBongController : Controller
    {
        private readonly QuanLySanBongDb _db = new QuanLySanBongDb();

        // ============================================================
        // GET: SanBong/Index
        // Hiển thị danh sách sân bóng với các bộ lọc
        // ============================================================
        public ActionResult Index(string loaiSan, string ngayDat, string tuKhoa)
        {
            // Lấy danh sách tất cả các sân ở trạng thái "Hoạt động" từ ADO.NET helper
            List<SanBong> listSan = _db.LayDanhSachSanBong(chiHoatDong: true);
            List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();

            // Ánh xạ sang SanBongViewModel để hiển thị kèm tên loại sân
            var viewModels = listSan.Select(s => new SanBongViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                MaLoai = s.MaLoai,
                TenLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.TenLoai,
                MoTaLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.MoTa,
                GiaTheoGio = s.GiaTheoGio,
                HinhAnh = s.HinhAnh,
                TrangThai = s.TrangThai
            }).ToList();

            // Filter theo loại sân (tên loại sân, ví dụ: "Sân 5 người")
            if (!string.IsNullOrEmpty(loaiSan))
            {
                viewModels = viewModels.Where(v => v.TenLoai != null && 
                    v.TenLoai.IndexOf(loaiSan, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Filter theo từ khóa (tìm kiếm theo tên sân)
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                viewModels = viewModels.Where(v => v.TenSan != null && 
                    v.TenSan.IndexOf(tuKhoa, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            // Lưu các tham số lọc vào ViewBag để hiển thị lại trên Form
            ViewBag.DanhSachLoai = listLoai;
            ViewBag.LoaiSanChon = loaiSan;
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.NgayDat = ngayDat;

            return View(viewModels);
        }

        // ============================================================
        // GET: SanBong/Detail/5
        // Hiển thị chi tiết sân bóng và lịch trống trong ngày
        // ============================================================
        public ActionResult Detail(int id, string ngayDat)
        {
            SanBong s = _db.LaySanBongTheoMa(id);
            if (s == null)
            {
                return HttpNotFound();
            }

            List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();

            var vm = new SanBongViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                MaLoai = s.MaLoai,
                TenLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.TenLoai,
                MoTaLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.MoTa,
                GiaTheoGio = s.GiaTheoGio,
                HinhAnh = s.HinhAnh,
                TrangThai = s.TrangThai
            };

            // Xác định ngày kiểm tra lịch đặt (mặc định là ngày mai nếu không truyền hợp lệ)
            DateTime selectedDate;
            if (!DateTime.TryParse(ngayDat, out selectedDate))
            {
                selectedDate = DateTime.Today.AddDays(1);
            }

            // Lấy tất cả các đơn đặt sân đang có của sân này vào ngày selectedDate (trừ đơn Đã hủy)
            var bookings = _db.LayTatCaDatSan()
                .Where(d => d.MaSan == id && d.NgayDat.Date == selectedDate.Date && d.TrangThai != "Đã hủy")
                .ToList();

            // Tạo danh sách khung giờ giả lập từ 00:00 đến 24:00 (mỗi slot 1 giờ)
            var slots = new List<TimeSlotViewModel>();
            for (int hour = 0; hour < 24; hour++)
            {
                var start = new TimeSpan(hour, 0, 0);
                var end = (hour == 23) ? new TimeSpan(23, 59, 59) : new TimeSpan(hour + 1, 0, 0);

                // Kiểm tra xem khung giờ này có bị trùng với đơn đặt nào không
                bool isBooked = bookings.Any(b => b.GioBatDau < end && b.GioKetThuc > start);

                slots.Add(new TimeSlotViewModel
                {
                    BatDau = start,
                    KetThuc = (hour == 23) ? new TimeSpan(24, 0, 0) : end,
                    DaDat = isBooked
                });
            }

            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.Slots = slots;

            return View(vm);
        }
    }
}
