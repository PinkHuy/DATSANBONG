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

        // ============================================================
        // GET: DatSan/DatSan
        // Hiển thị form đặt sân
        // ============================================================
        [HttpGet]
        public ActionResult DatSan(int maSan, string ngayDat, string gioBatDau, string gioKetThuc)
        {
            // Kiểm tra trạng thái đăng nhập
            if (Session["MaND"] == null)
            {
                TempData["LoiThongBao"] = "Vui lòng đăng nhập để thực hiện đặt sân.";
                return RedirectToAction("DangNhap", "Account", new { returnUrl = Request.RawUrl });
            }

            SanBong s = _db.LaySanBongTheoMa(maSan);
            if (s == null)
            {
                return HttpNotFound();
            }

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

            // Điền sẵn dữ liệu từ Lịch trống Detail nếu có
            DateTime dateVal;
            if (DateTime.TryParse(ngayDat, out dateVal))
            {
                vm.NgayDat = dateVal;
            }
            else
            {
                vm.NgayDat = DateTime.Today.AddDays(1);
            }

            if (!string.IsNullOrEmpty(gioBatDau))
            {
                vm.GioBatDauStr = gioBatDau;
            }
            if (!string.IsNullOrEmpty(gioKetThuc))
            {
                vm.GioKetThucStr = gioKetThuc;
            }

            return View(vm);
        }

        // ============================================================
        // POST: DatSan/DatSan
        // Xử lý xác nhận đặt sân
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatSan(DatSanFormViewModel vm)
        {
            if (Session["MaND"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            TimeSpan tStart;
            TimeSpan tEnd;

            bool isEnd24 = vm.GioKetThucStr == "24:00";

            // Parse giờ bắt đầu và giờ kết thúc
            if (!TimeSpan.TryParse(vm.GioBatDauStr, out tStart))
            {
                ModelState.AddModelError("GioBatDauStr", "Giờ bắt đầu không hợp lệ.");
            }

            if (isEnd24)
            {
                // Lưu DB với 23:59:59 để tránh lỗi tràn time trong SQL Server
                tEnd = new TimeSpan(23, 59, 59);
            }
            else if (!TimeSpan.TryParse(vm.GioKetThucStr, out tEnd))
            {
                ModelState.AddModelError("GioKetThucStr", "Giờ kết thúc không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                // Validate ngày đặt >= hôm nay
                if (vm.NgayDat.Date < DateTime.Today)
                {
                    ModelState.AddModelError("NgayDat", "Ngày đặt không được ở quá khứ.");
                }

                // Validate giờ kết thúc > giờ bắt đầu
                TimeSpan tEndForCompare = isEnd24 ? new TimeSpan(24, 0, 0) : tEnd;
                if (tEndForCompare <= tStart)
                {
                    ModelState.AddModelError("GioKetThucStr", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
                }

                // Kiểm tra trùng lịch có xét phân cấp cụm sân (sân lớn - sân nhỏ)
                bool isOverlapped = _db.KiemTraTrungLichPhanCap(vm.MaSan, vm.NgayDat, tStart, tEndForCompare);

                if (isOverlapped)
                {
                    ModelState.AddModelError("", "Khung giờ bạn chọn đã bị trùng lịch với một người đặt khác (hoặc trùng với lịch của cụm sân liên quan). Vui lòng chọn khung giờ khác.");
                }

                if (ModelState.IsValid)
                {
                    // Tính tổng tiền dựa trên số giờ đặt
                    double hours = (tEndForCompare - tStart).TotalHours;
                    decimal tongTien = vm.GiaTheoGio * (decimal)hours;

                    var datSan = new DatSan
                    {
                        MaND = (int)Session["MaND"],
                        MaSan = vm.MaSan,
                        NgayDat = vm.NgayDat,
                        GioBatDau = tStart,
                        GioKetThuc = tEnd, // Lưu giờ kết thúc (23:59:59 nếu là 24:00)
                        TongTien = tongTien,
                        GhiChu = vm.GhiChu?.Trim(),
                        TrangThai = "Chờ duyệt"
                    };

                    _db.ThemDatSan(datSan);

                    TempData["ThanhCong"] = "Đặt sân thành công! Đơn đặt của bạn đang chờ Admin phê duyệt.";
                    return RedirectToAction("LichSu");
                }
            }

            // Nếu xảy ra lỗi, tải lại thông tin sân bóng để hiển thị lại view
            SanBong s = _db.LaySanBongTheoMa(vm.MaSan);
            if (s != null)
            {
                List<LoaiSan> listLoai = _db.LayDanhSachLoaiSan();
                vm.TenSan = s.TenSan;
                vm.TenLoai = listLoai.FirstOrDefault(l => l.MaLoai == s.MaLoai)?.TenLoai;
                vm.GiaTheoGio = s.GiaTheoGio;
                vm.HinhAnh = s.HinhAnh;
            }

            return View(vm);
        }

        // ============================================================
        // GET: DatSan/LichSu
        // Hiển thị lịch sử đặt sân của người dùng đang đăng nhập
        // ============================================================
        public ActionResult LichSu()
        {
            if (Session["MaND"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

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

        // ============================================================
        // POST: DatSan/HuyDatSan/5
        // Khách hàng hủy yêu cầu đặt sân đang ở trạng thái "Chờ duyệt"
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDatSan(int id)
        {
            if (Session["MaND"] == null)
            {
                return RedirectToAction("DangNhap", "Account");
            }

            DatSan ds = _db.LayDatSanTheoMa(id);
            if (ds != null)
            {
                // Kiểm tra xem đơn đặt sân có thuộc về người dùng đang đăng nhập không
                if (ds.MaND == (int)Session["MaND"])
                {
                    if (ds.TrangThai == "Chờ duyệt")
                    {
                        _db.CapNhatTrangThaiDatSan(id, "Đã hủy");
                        TempData["ThanhCong"] = "Hủy yêu cầu đặt sân thành công!";
                    }
                    else
                    {
                        TempData["LoiThongBao"] = "Không thể hủy đơn đặt sân đã được phê duyệt hoặc đã bị hủy trước đó.";
                    }
                }
                else
                {
                    TempData["LoiThongBao"] = "Bạn không có quyền thực hiện thao tác này.";
                }
            }
            else
            {
                TempData["LoiThongBao"] = "Không tìm thấy thông tin đơn đặt sân.";
            }

            return RedirectToAction("LichSu");
        }

        // ============================================================
        // GET: DatSan/GetEventsJson
        // Lấy danh sách lịch đặt sân định dạng JSON cho FullCalendar
        // ============================================================
        [HttpGet]
        public JsonResult GetEventsJson(int? maSan, string start, string end)
        {
            DateTime startDate;
            DateTime endDate;

            if (!DateTime.TryParse(start, out startDate)) startDate = DateTime.Today.AddDays(-7);
            if (!DateTime.TryParse(end, out endDate)) endDate = DateTime.Today.AddDays(7);

            var listSan = _db.LayDanhSachSanBong();
            var bookings = _db.LayTatCaDatSan()
                .Where(d => d.NgayDat >= startDate && d.NgayDat <= endDate && d.TrangThai != "Đã hủy");

            if (maSan.HasValue)
            {
                // Tìm các sân liên quan (bản thân sân này, sân cha, sân con)
                var relatedSanIds = new List<int> { maSan.Value };
                var san = _db.LaySanBongTheoMa(maSan.Value);
                if (san != null)
                {
                    if (san.MaSanCha.HasValue)
                    {
                        relatedSanIds.Add(san.MaSanCha.Value);
                    }
                    var sanConIds = listSan.Where(s => s.MaSanCha == maSan.Value).Select(s => s.MaSan).ToList();
                    relatedSanIds.AddRange(sanConIds);
                }

                bookings = bookings.Where(d => d.MaSan.HasValue && relatedSanIds.Contains(d.MaSan.Value));
            }

            var events = bookings.Select(b => {
                // Điều chỉnh giờ kết thúc 23:59:59 -> 24:00:00 cho FullCalendar hiển thị đẹp
                string timeEndStr = b.GioKetThuc.ToString(@"hh\:mm\:ss");
                if (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59 && b.GioKetThuc.Seconds == 59)
                {
                    timeEndStr = "24:00:00";
                }

                var bSan = listSan.FirstOrDefault(s => s.MaSan == b.MaSan);
                string tenSan = bSan?.TenSan ?? "Sân N/A";
                bool isCurrentSan = maSan.HasValue && b.MaSan == maSan.Value;
                string title = isCurrentSan ? (b.TrangThai == "Đã duyệt" ? "Đã đặt" : "Chờ duyệt") : $"Bận (trùng {tenSan})";
                
                // Chọn màu sắc: Đã duyệt (Xanh lá), Chờ duyệt (Cam), Sân khác trùng lịch (Đỏ/Xám)
                string color = "#10b981"; // Đã duyệt (Xanh lá)
                string border = "#047857";
                if (b.TrangThai == "Chờ duyệt")
                {
                    color = "#f59e0b"; // Chờ duyệt (Vàng cam)
                    border = "#d97706";
                }
                if (!isCurrentSan)
                {
                    color = "#ef4444"; // Trùng sân khác (Đỏ)
                    border = "#b91c1c";
                }

                return new {
                    id = b.MaDatSan,
                    title = title,
                    start = b.NgayDat.ToString("yyyy-MM-dd") + "T" + b.GioBatDau.ToString(@"hh\:mm\:ss"),
                    end = b.NgayDat.ToString("yyyy-MM-dd") + "T" + timeEndStr,
                    backgroundColor = color,
                    borderColor = border,
                    textColor = "#ffffff",
                    extendedProps = new {
                        tenSan = tenSan,
                        trangThai = b.TrangThai,
                        isCurrent = isCurrentSan
                    }
                };
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }

        // ============================================================
        // GET: DatSan/GetLichSanBong
        // API lấy lịch đặt sân của một sân bóng cụ thể (VẤN ĐỀ 1)
        // ============================================================
        [HttpGet]
        public JsonResult GetLichSanBong(int maSan)
        {
            // Dùng LINQ truy vấn bảng đơn đặt sân, lọc theo MaSan và trạng thái khác "Đã hủy"
            var bookings = _db.LayTatCaDatSan()
                .Where(d => d.MaSan == maSan && d.TrangThai != "Đã hủy");

            // Ánh xạ dữ liệu sang cấu trúc JSON chuẩn của FullCalendar
            var events = bookings.Select(b => {
                // Điều chỉnh thời gian kết thúc 23:59:59 thành 24:00:00 để hiển thị mượt mà trên FullCalendar
                string timeEndStr = b.GioKetThuc.ToString(@"hh\:mm\:ss");
                if (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59 && b.GioKetThuc.Seconds == 59)
                {
                    timeEndStr = "24:00:00";
                }

                return new {
                    title = "Đã có người đặt", // Bảo mật tên khách hàng khác
                    start = b.NgayDat.ToString("yyyy-MM-dd") + "T" + b.GioBatDau.ToString(@"hh\:mm\:ss"),
                    end = b.NgayDat.ToString("yyyy-MM-dd") + "T" + timeEndStr,
                    color = "#e74c3c", // Màu đỏ nhạt biểu thị khung giờ bị khóa
                    textColor = "#ffffff"
                };
            }).ToList();

            return Json(events, JsonRequestBehavior.AllowGet);
        }
    }
}
