using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATSANBONG.Data;
using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    public class DatSanController : Controller
    {
        private readonly QuanLySanBongDb _db = new QuanLySanBongDb();

        [HttpGet]
        public ActionResult DatSan(int maSan, string ngayDat, string gioBatDau, string gioKetThuc)
        {
            if (Session["MaND"] == null)
            {
                TempData["LoiThongBao"] = "Vui lòng đăng nhập để thực hiện đặt sân.";
                return RedirectToAction("DangNhap", "Account", new { returnUrl = Request.RawUrl });
            }

            SanBong s = _db.LaySanBongTheoMa(maSan);
            if (s == null) return HttpNotFound();

            List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();
            string tenLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.TenLoai;

            var vm = new DatSanFormViewModel
            {
                MaSan = s.MaSan,
                TenSan = s.TenSan,
                TenLoai = tenLoai,
                GiaTheoGio = s.GiaTheoGio,
                HinhAnh = s.HinhAnh
            };

            DateTime dateVal;
            vm.NgayDat = DateTime.TryParse(ngayDat, out dateVal) ? dateVal : DateTime.Today.AddDays(1);

            if (!string.IsNullOrEmpty(gioBatDau)) vm.GioBatDauStr = gioBatDau;
            if (!string.IsNullOrEmpty(gioKetThuc)) vm.GioKetThucStr = gioKetThuc;

            vm.DanhSachSanCon = _db.LaySanConTheoMaSan(maSan);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatSan(DatSanFormViewModel vm)
        {
            if (Session["MaND"] == null)
                return RedirectToAction("DangNhap", "Account");

            TimeSpan tStart;
            TimeSpan tEnd;
            bool isEnd24 = vm.GioKetThucStr == "24:00";

            if (!TimeSpan.TryParse(vm.GioBatDauStr, out tStart))
                ModelState.AddModelError("GioBatDauStr", "Giờ bắt đầu không hợp lệ.");

            if (isEnd24)
                tEnd = new TimeSpan(23, 59, 59);
            else if (!TimeSpan.TryParse(vm.GioKetThucStr, out tEnd))
                ModelState.AddModelError("GioKetThucStr", "Giờ kết thúc không hợp lệ.");

            if (ModelState.IsValid)
            {
                if (vm.NgayDat.Date < DateTime.Today)
                    ModelState.AddModelError("NgayDat", "Ngày đặt không được ở quá khứ.");

                TimeSpan tEndForCompare = isEnd24 ? new TimeSpan(24, 0, 0) : tEnd;
                if (tEndForCompare <= tStart)
                    ModelState.AddModelError("GioKetThucStr", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");

                // Lấy tên sân con người dùng chọn: "A1","A2","A3","A4" hoặc null
                string sanConChon = !string.IsNullOrEmpty(vm.TenSanCon) ? vm.TenSanCon.Trim() : null;

                // Kiểm tra trùng lịch:
                // - Có chọn sân con → chỉ kiểm tra đúng sân con đó (A1 và A2 cùng giờ vẫn OK)
                // - Không chọn sân con → kiểm tra toàn bộ sân cha
                bool isOverlapped = !string.IsNullOrEmpty(sanConChon)
                    ? _db.KiemTraTrungLichSanCon(vm.MaSan, sanConChon, vm.NgayDat, tStart, tEndForCompare)
                    : _db.KiemTraTrungLichPhanCap(vm.MaSan, vm.NgayDat, tStart, tEndForCompare);

                if (isOverlapped)
                {
                    string msg = !string.IsNullOrEmpty(sanConChon)
                        ? $"Sân {sanConChon} đã được đặt trong khung giờ này. Vui lòng chọn sân con khác."
                        : "Khung giờ bạn chọn đã bị trùng lịch. Vui lòng chọn khung giờ khác.";
                    ModelState.AddModelError("", msg);
                }

                if (ModelState.IsValid)
                {
                    double hours = (tEndForCompare - tStart).TotalHours;
                    decimal tongTien = vm.GiaTheoGio * (decimal)hours;

                    var datSan = new DatSan
                    {
                        MaND = (int)Session["MaND"],
                        MaSan = vm.MaSan,
                        NgayDat = vm.NgayDat,
                        GioBatDau = tStart,
                        GioKetThuc = tEnd,
                        TongTien = tongTien,
                        GhiChu = vm.GhiChu?.Trim(),
                        TrangThai = "Chờ duyệt",
                        MaSanCon = sanConChon  // "A1","A2","A3","A4" hoặc null
                    };

                    _db.ThemDatSan(datSan);

                    TempData["ThanhCong"] = "Đặt sân thành công! Đơn đặt của bạn đang chờ Admin phê duyệt.";
                    return RedirectToAction("LichSu");
                }
            }

            // Tải lại thông tin sân nếu có lỗi
            SanBong s = _db.LaySanBongTheoMa(vm.MaSan);
            if (s != null)
            {
                List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();
                vm.TenSan = s.TenSan;
                vm.TenLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.TenLoai;
                vm.GiaTheoGio = s.GiaTheoGio;
                vm.HinhAnh = s.HinhAnh;
                vm.DanhSachSanCon = _db.LaySanConTheoMaSan(vm.MaSan);
            }

            return View(vm);
        }

        public ActionResult LichSu()
        {
            if (Session["MaND"] == null)
                return RedirectToAction("DangNhap", "Account");

            int maND = (int)Session["MaND"];
            List<DatSan> listDatSan = _db.LayLichSuDatSanTheoNguoiDung(maND);
            List<SanBong> listSan = _db.LayDanhSachSanBong();
            List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();

            var viewModels = listDatSan.Select(d => {
                var san = listSan.FirstOrDefault(s => s.MaSan == d.MaSan);
                var loai = san != null ? listLoai.FirstOrDefault(l => l.MaLoai == san.MaLoai) : null;

                return new DatSanViewModel
                {
                    MaDatSan = d.MaDatSan,
                    MaND = d.MaND,
                    MaSan = d.MaSan,
                    NgayDat = d.NgayDat,
                    GioBatDau = d.GioBatDau,
                    GioKetThuc = d.GioKetThuc,
                    TongTien = d.TongTien,
                    GhiChu = d.GhiChu,
                    TrangThai = d.TrangThai,
                    TenSan = san?.TenSan ?? "Sân đã bị xóa",
                    HinhAnh = san?.HinhAnh,
                    GiaTheoGio = san?.GiaTheoGio ?? 0,
                    TenLoai = loai?.TenLoai ?? "N/A"
                };
            }).ToList();

            return View(viewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDatSan(int id)
        {
            if (Session["MaND"] == null)
                return RedirectToAction("DangNhap", "Account");

            DatSan ds = _db.LayDatSanTheoMa(id);
            if (ds != null)
            {
                if (ds.MaND == (int)Session["MaND"])
                {
                    if (ds.TrangThai == "Chờ duyệt")
                    {
                        _db.CapNhatTrangThaiDatSan(id, "Đã hủy");
                        TempData["ThanhCong"] = "Hủy yêu cầu đặt sân thành công!";
                    }
                    else
                        TempData["LoiThongBao"] = "Không thể hủy đơn đặt sân đã được phê duyệt hoặc đã bị hủy trước đó.";
                }
                else
                    TempData["LoiThongBao"] = "Bạn không có quyền thực hiện thao tác này.";
            }
            else
                TempData["LoiThongBao"] = "Không tìm thấy thông tin đơn đặt sân.";

            return RedirectToAction("LichSu");
        }

        [HttpGet]
        public JsonResult GetEventsJson(int? maSan, string start, string end)
        {
            DateTime startDate, endDate;
            if (!DateTime.TryParse(start, out startDate)) startDate = DateTime.Today.AddDays(-7);
            if (!DateTime.TryParse(end, out endDate)) endDate = DateTime.Today.AddDays(7);

            var listSan = _db.LayDanhSachSanBong();
            var bookings = _db.LayTatCaDatSan()
                .Where(d => d.NgayDat >= startDate && d.NgayDat <= endDate && d.TrangThai != "Đã hủy");

            if (maSan.HasValue)
            {
                var relatedSanIds = new List<int> { maSan.Value };
                var san = _db.LaySanBongTheoMa(maSan.Value);
                if (san != null)
                {
                    if (san.MaSanCha.HasValue) relatedSanIds.Add(san.MaSanCha.Value);
                    relatedSanIds.AddRange(listSan.Where(s => s.MaSanCha == maSan.Value).Select(s => s.MaSan));
                }
                // FIX CS1503/CS0019: d.MaSan là int?, dùng .HasValue + .Value
                bookings = bookings.Where(d => d.MaSan.HasValue && relatedSanIds.Contains(d.MaSan.Value));
            }

            var events = bookings.Select(b => {
                string timeEndStr = b.GioKetThuc.ToString(@"hh\:mm\:ss");
                if (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59 && b.GioKetThuc.Seconds == 59)
                    timeEndStr = "24:00:00";

                // FIX CS1503: b.MaSan là int?, listSan.MaSan là int → dùng b.MaSan.HasValue
                var bSan = b.MaSan.HasValue ? listSan.FirstOrDefault(s => s.MaSan == b.MaSan.Value) : null;
                string tenSan = bSan?.TenSan ?? "Sân N/A";
                // FIX CS0019: so sánh int? với int? → dùng == trực tiếp hoặc .Value
                bool isCurrentSan = maSan.HasValue && b.MaSan.HasValue && b.MaSan.Value == maSan.Value;

                // b.MaSanCon giờ là string → string.IsNullOrEmpty đúng rồi
                string title = isCurrentSan
                    ? (b.TrangThai == "Đã duyệt" ? "Đã đặt" : "Chờ duyệt")
                      + (!string.IsNullOrEmpty(b.MaSanCon) ? $" ({b.MaSanCon})" : "")
                    : $"Bận (trùng {tenSan})";

                string color = "#10b981";
                string border = "#047857";
                if (b.TrangThai == "Chờ duyệt") { color = "#f59e0b"; border = "#d97706"; }
                if (!isCurrentSan) { color = "#ef4444"; border = "#b91c1c"; }

                return new
                {
                    id = b.MaDatSan,
                    title = title,
                    start = b.NgayDat.ToString("yyyy-MM-dd") + "T" + b.GioBatDau.ToString(@"hh\:mm\:ss"),
                    end = b.NgayDat.ToString("yyyy-MM-dd") + "T" + timeEndStr,
                    backgroundColor = color,
                    borderColor = border,
                    textColor = "#ffffff",
                    extendedProps = new { tenSan, trangThai = b.TrangThai, isCurrent = isCurrentSan, sanCon = b.MaSanCon }
                };
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLichSanBong(int maSan, string maSanCon = null)
        {
            // FIX CS1503: d.MaSan là int?, so sánh với int maSan → dùng d.MaSan == maSan (int? == int tự lift)
            var bookings = _db.LayTatCaDatSan()
                .Where(d => d.MaSan.HasValue && d.MaSan.Value == maSan && d.TrangThai != "Đã hủy");

            if (!string.IsNullOrEmpty(maSanCon))
                // FIX CS1503: d.MaSanCon giờ là string → == string OK
                bookings = bookings.Where(d => d.MaSanCon == maSanCon);

            var events = bookings.Select(b => {
                string timeEndStr = b.GioKetThuc.ToString(@"hh\:mm\:ss");
                if (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59 && b.GioKetThuc.Seconds == 59)
                    timeEndStr = "24:00:00";

                // b.MaSanCon là string → OK
                string title = !string.IsNullOrEmpty(b.MaSanCon)
                    ? $"Sân {b.MaSanCon} - Đã đặt"
                    : "Đã có người đặt";

                return new
                {
                    title,
                    start = b.NgayDat.ToString("yyyy-MM-dd") + "T" + b.GioBatDau.ToString(@"hh\:mm\:ss"),
                    end = b.NgayDat.ToString("yyyy-MM-dd") + "T" + timeEndStr,
                    color = "#e74c3c",
                    textColor = "#ffffff",
                    extendedProps = new { sanCon = b.MaSanCon }
                };
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }
    }
}