// ===================================================================
// ViewModels trung gian để hiển thị dữ liệu join (thay thế navigation
// properties bị chặn bởi LINQ to SQL EntityRef/EntitySet)
// ===================================================================
using System;
using System.ComponentModel.DataAnnotations;

namespace DATSANBONG.Models
{
    /// <summary>
    /// ViewModel cho danh sách / chi tiết Sân Bóng (có TenLoai từ join)
    /// </summary>
    public class SanBongViewModel
    {
        public int    MaSan      { get; set; }
        public string TenSan     { get; set; }
        public int?   MaLoai     { get; set; }
        public string TenLoai    { get; set; }
        public string MoTaLoai   { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string HinhAnh    { get; set; }
        public string TrangThai  { get; set; }
        public int?   MaSanCha   { get; set; }
        public string TenSanCha  { get; set; }

        public string GiaFormatted => GiaTheoGio.ToString("N0") + " ₫/giờ";
        public bool   DangHoatDong => TrangThai == "Hoạt động";
    }

    /// <summary>
    /// ViewModel cho lịch sử / chi tiết Đặt Sân (có TenSan, HoTen từ join)
    /// </summary>
    public class DatSanViewModel
    {
        public int      MaDatSan    { get; set; }
        public int?     MaND        { get; set; }
        public int?     MaSan       { get; set; }
        public DateTime NgayDat     { get; set; }
        public TimeSpan GioBatDau   { get; set; }
        public TimeSpan GioKetThuc  { get; set; }
        public decimal? TongTien    { get; set; }
        public string   GhiChu      { get; set; }
        public string   TrangThai   { get; set; }

        // Từ join
        public string TenSan     { get; set; }
        public string TenLoai    { get; set; }
        public string HinhAnh    { get; set; }
        public decimal GiaTheoGio { get; set; }

        // Người dùng (Admin view)
        public string HoTen      { get; set; }
        public string TaiKhoan   { get; set; }

        public string TongTienFormatted  => TongTien.HasValue ? TongTien.Value.ToString("N0") + " ₫" : "—";
        public string GioBatDauStr       => GioBatDau.ToString(@"hh\:mm");
        public string GioKetThucStr      => GioKetThuc.ToString(@"hh\:mm");
        public string NgayDatFormatted   => NgayDat.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// ViewModel cho form Đặt Sân (khách hàng điền)
    /// </summary>
    public class DatSanFormViewModel
    {
        public int MaSan { get; set; }

        // Thông tin sân (hiển thị, readonly)
        public string TenSan     { get; set; }
        public string TenLoai    { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string HinhAnh    { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đặt")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đặt")]
        public DateTime NgayDat { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d$", ErrorMessage = "Định dạng giờ HH:mm")]
        [Display(Name = "Giờ bắt đầu")]
        public string GioBatDauStr { get; set; } = "07:00";

        [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
        [RegularExpression(@"^([01]?\d|2[0-3]):[0-5]\d$", ErrorMessage = "Định dạng giờ HH:mm")]
        [Display(Name = "Giờ kết thúc")]
        public string GioKetThucStr { get; set; } = "09:00";

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        public decimal TongTienTinh { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng khung giờ đặt sân (khung giờ 1 tiếng)
    /// </summary>
    public class TimeSlotViewModel
    {
        public TimeSpan BatDau { get; set; }
        public TimeSpan KetThuc { get; set; }
        public bool DaDat { get; set; }

        public string KhungGioStr => $"{BatDau.ToString(@"hh\:mm")} - {(KetThuc.Days > 0 ? "24:00" : KetThuc.ToString(@"hh\:mm"))}";
    }
}
