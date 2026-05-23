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

                // Kiểm tra xem khung giờ này có bị trùng lặp lịch với đơn đặt nào đã được duyệt hoặc đang chờ duyệt hay không
                var existingBookings = _db.LayTatCaDatSan()
                    .Where(d => d.MaSan == vm.MaSan && d.NgayDat.Date == vm.NgayDat.Date && d.TrangThai != "Đã hủy")
                    .ToList();

                bool isOverlapped = existingBookings.Any(b => {
                    // Nếu giờ kết thúc trong DB là 23:59:59 và slot đó là 23:00 - 24:00, ta coi giờ kết thúc thực tế so sánh là 24:00
                    TimeSpan bEndCompare = (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59) ? new TimeSpan(24, 0, 0) : b.GioKetThuc;
                    return b.GioBatDau < tEndForCompare && bEndCompare > tStart;
                });

                if (isOverlapped)
                {
                    ModelState.AddModelError("", "Khung giờ bạn chọn đã bị trùng lịch với một người đặt khác. Vui lòng chọn khung giờ khác.");
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
    }
}
