// ===================================================================
// File này khai báo các "metadata buddy class" để thêm Data Annotations
// vào các partial class được tự sinh bởi DataClasses1.designer.cs
// (LINQ to SQL Designer). KHÔNG định nghĩa lại class — chỉ bổ sung.
// ===================================================================
using System.ComponentModel.DataAnnotations;

namespace DATSANBONG.Models
{
    // ── Metadata companion cho partial class NguoiDung ──────────────
    [MetadataType(typeof(NguoiDungMetadata))]
    public partial class NguoiDung { }

    public class NguoiDungMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tài khoản")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tài khoản từ 4–50 ký tự")]
        [Display(Name = "Tài khoản")]
        public string TaiKhoan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; }
    }

    // ── Metadata companion cho partial class LoaiSan ────────────────
    [MetadataType(typeof(LoaiSanMetadata))]
    public partial class LoaiSan { }

    public class LoaiSanMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên loại sân")]
        [StringLength(100)]
        [Display(Name = "Tên loại sân")]
        public string TenLoai { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }
    }

    // ── Metadata companion cho partial class SanBong ────────────────
    [MetadataType(typeof(SanBongMetadata))]
    public partial class SanBong { }

    public class SanBongMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sân")]
        [StringLength(200)]
        [Display(Name = "Tên sân")]
        public string TenSan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sân")]
        [Display(Name = "Loại sân")]
        public int MaLoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá theo giờ")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá theo giờ (VNĐ)")]
        public decimal GiaTheoGio { get; set; }

        [Display(Name = "Hình ảnh")]
        public string HinhAnh { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; }
    }

    // ── Metadata companion cho partial class DatSan ─────────────────
    [MetadataType(typeof(DatSanMetadata))]
    public partial class DatSan { }

    public class DatSanMetadata
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày đặt")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đặt")]
        public System.DateTime NgayDat { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu")]
        [Display(Name = "Giờ bắt đầu")]
        public System.TimeSpan GioBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc")]
        [Display(Name = "Giờ kết thúc")]
        public System.TimeSpan GioKetThuc { get; set; }

        [Display(Name = "Tổng tiền (VNĐ)")]
        public decimal? TongTien { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; }
    }
}
