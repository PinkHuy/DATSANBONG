using System;
using System.Collections.Generic;

namespace DATSANBONG.Models
{
    public class DatSanFormViewModel
    {
        public int MaSan { get; set; }
        public string TenSan { get; set; }
        public string HinhAnh { get; set; }
        public string TenLoai { get; set; }
        public decimal GiaTheoGio { get; set; }

        public DateTime NgayDat { get; set; }
        public string GioBatDauStr { get; set; }
        public string GioKetThucStr { get; set; }

        /// <summary>
        /// Tên sân con người dùng chọn: "A1", "A2", "A3", "A4" hoặc null nếu không chọn.
        /// Dùng string thay vì int? để khớp với cột maSanCon (nvarchar) trong DB.
        /// </summary>
        public string TenSanCon { get; set; }

        public string GhiChu { get; set; }

        public List<SanCon> DanhSachSanCon { get; set; }
    }

    public class DatSanViewModel
    {
        public int MaDatSan { get; set; }
        public int? MaND { get; set; }
        public int? MaSan { get; set; }
        public DateTime NgayDat { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public decimal? TongTien { get; set; }
        public string GhiChu { get; set; }
        public string TrangThai { get; set; }

        public string TenSan { get; set; }
        public string HinhAnh { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string TenLoai { get; set; }
        public string NgayDatFormatted => NgayDat.ToString("dd/MM/yyyy");
        public string TongTienFormatted => TongTien.HasValue ? TongTien.Value.ToString("N0") + " ₫" : "0 ₫";
    }

    public class SanBongViewModel
    {
        public int MaSan { get; set; }
        public string TenSan { get; set; }
        public int? MaLoai { get; set; }
        public string TenLoai { get; set; }
        public string MoTaLoai { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string HinhAnh { get; set; }
        public string TrangThai { get; set; }
        public int? MaSanCha { get; set; }
        public string TenSanCha { get; set; }

        // ← Thêm mới: dùng trong Index.cshtml và Detail.cshtml
        public string GiaFormatted => GiaTheoGio.ToString("N0") + " ₫/giờ";
    }

    public class TimeSlotViewModel
    {
        public TimeSpan BatDau { get; set; }
        public TimeSpan KetThuc { get; set; }
        public bool DaDat { get; set; }

        // ← Thêm mới: dùng trong Detail.cshtml
        public string KhungGioStr =>
            $"{BatDau:hh\\:mm} - {KetThuc:hh\\:mm}";
    }
}