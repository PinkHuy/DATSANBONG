using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using DATSANBONG.Models;

namespace DATSANBONG.Data
{
    /// <summary>
    /// Lớp truy cập dữ liệu trung tâm sử dụng ADO.NET
    /// Cung cấp helper methods cho tất cả bảng trong database QuanLySanBong_MVC
    /// </summary>
    public class QuanLySanBongDb
    {
        private readonly string _connectionString;

        public QuanLySanBongDb()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["QuanLySanBongDb"].ConnectionString;
        }

        /// <summary>Tạo SqlConnection mới từ connection string</summary>
        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // ─────────────────────────────────────────────────────────────
        //  HASH MẬT KHẨU (SHA-256)
        // ─────────────────────────────────────────────────────────────

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  NGƯỜI DÙNG
        // ─────────────────────────────────────────────────────────────

        /// <summary>Lấy người dùng theo tài khoản và mật khẩu (đã hash)</summary>
        public NguoiDung DangNhap(string taiKhoan, string matKhauHash)
        {
            const string sql = @"
                SELECT MaND, HoTen, TaiKhoan, MatKhau, SoDienThoai, VaiTro
                FROM   NguoiDung
                WHERE  TaiKhoan = @TaiKhoan AND MatKhau = @MatKhau";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                cmd.Parameters.AddWithValue("@MatKhau", matKhauHash);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return MapNguoiDung(reader);
                }
            }
            return null;
        }

        /// <summary>Kiểm tra tài khoản đã tồn tại chưa</summary>
        public bool TaiKhoanDaTonTai(string taiKhoan)
        {
            const string sql = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan = @TaiKhoan";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        /// <summary>Thêm người dùng mới, trả về MaND mới</summary>
        public int ThemNguoiDung(NguoiDung nd)
        {
            const string sql = @"
                INSERT INTO NguoiDung (HoTen, TaiKhoan, MatKhau, SoDienThoai, VaiTro)
                VALUES (@HoTen, @TaiKhoan, @MatKhau, @SoDienThoai, @VaiTro);
                SELECT SCOPE_IDENTITY();";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@HoTen", nd.HoTen);
                cmd.Parameters.AddWithValue("@TaiKhoan", nd.TaiKhoan);
                cmd.Parameters.AddWithValue("@MatKhau", nd.MatKhau);
                cmd.Parameters.AddWithValue("@SoDienThoai", (object)nd.SoDienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VaiTro", nd.VaiTro ?? "Khách hàng");
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>Lấy tất cả người dùng (dùng cho Admin)</summary>
        public List<NguoiDung> LayDanhSachNguoiDung()
        {
            const string sql = "SELECT MaND, HoTen, TaiKhoan, MatKhau, SoDienThoai, VaiTro FROM NguoiDung ORDER BY MaND";
            return QueryList(sql, null, MapNguoiDung);
        }

        /// <summary>Lấy người dùng theo MaND</summary>
        public NguoiDung LayNguoiDungTheoMa(int maND)
        {
            const string sql = "SELECT MaND, HoTen, TaiKhoan, MatKhau, SoDienThoai, VaiTro FROM NguoiDung WHERE MaND = @MaND";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaND", maND);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) return MapNguoiDung(reader);
                }
            }
            return null;
        }

        /// <summary>Xoá người dùng theo MaND</summary>
        public void XoaNguoiDung(int maND)
        {
            // Xoá các đơn đặt sân của khách hàng trước để tránh lỗi ràng buộc khoá ngoại (Foreign Key Constraint)
            const string sqlDatSan = "DELETE FROM DatSan WHERE MaND = @MaND";
            ExecuteNonQuery(sqlDatSan, cmd => cmd.Parameters.AddWithValue("@MaND", maND));

            const string sql = "DELETE FROM NguoiDung WHERE MaND = @MaND";
            ExecuteNonQuery(sql, cmd => cmd.Parameters.AddWithValue("@MaND", maND));
        }

        // ─────────────────────────────────────────────────────────────
        //  LOẠI SÂN
        // ─────────────────────────────────────────────────────────────

        public List<LoaiSan> LayDanhSachLoaiSan()
        {
            const string sql = "SELECT MaLoai, TenLoai, MoTa FROM LoaiSan ORDER BY MaLoai";
            return QueryList(sql, null, r => new LoaiSan
            {
                MaLoai = (int)r["MaLoai"],
                TenLoai = r["TenLoai"].ToString(),
                MoTa = r["MoTa"] == DBNull.Value ? null : r["MoTa"].ToString()
            });
        }

        public LoaiSan LayLoaiSanTheoMa(int maLoai)
        {
            const string sql = "SELECT MaLoai, TenLoai, MoTa FROM LoaiSan WHERE MaLoai = @MaLoai";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaLoai", maLoai);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) return new LoaiSan
                    {
                        MaLoai = (int)r["MaLoai"],
                        TenLoai = r["TenLoai"].ToString(),
                        MoTa = r["MoTa"] == DBNull.Value ? null : r["MoTa"].ToString()
                    };
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────
        //  SÂN BÓNG
        // ─────────────────────────────────────────────────────────────

        public List<SanBong> LayDanhSachSanBong(bool chiHoatDong = false)
        {
            string sql = @"
                SELECT s.MaSan, s.TenSan, s.MaLoai, s.GiaTheoGio, s.HinhAnh, s.TrangThai,
                       l.TenLoai, l.MoTa AS MoTaLoai
                FROM   SanBong s
                JOIN   LoaiSan l ON s.MaLoai = l.MaLoai";
            if (chiHoatDong) sql += " WHERE s.TrangThai = N'Hoạt động'";
            sql += " ORDER BY s.MaSan";

            return QueryList(sql, null, MapSanBong);
        }

        public SanBong LaySanBongTheoMa(int maSan)
        {
            const string sql = @"
                SELECT s.MaSan, s.TenSan, s.MaLoai, s.GiaTheoGio, s.HinhAnh, s.TrangThai,
                       l.TenLoai, l.MoTa AS MoTaLoai
                FROM   SanBong s
                JOIN   LoaiSan l ON s.MaLoai = l.MaLoai
                WHERE  s.MaSan = @MaSan";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaSan", maSan);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) return MapSanBong(r);
                }
            }
            return null;
        }

        public int ThemSanBong(SanBong san)
        {
            const string sql = @"
                INSERT INTO SanBong (TenSan, MaLoai, GiaTheoGio, HinhAnh, TrangThai)
                VALUES (@TenSan, @MaLoai, @GiaTheoGio, @HinhAnh, @TrangThai);
                SELECT SCOPE_IDENTITY();";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TenSan", san.TenSan);
                cmd.Parameters.AddWithValue("@MaLoai", san.MaLoai);
                cmd.Parameters.AddWithValue("@GiaTheoGio", san.GiaTheoGio);
                cmd.Parameters.AddWithValue("@HinhAnh", (object)san.HinhAnh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", san.TrangThai ?? "Hoạt động");
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void CapNhatSanBong(SanBong san)
        {
            const string sql = @"
                UPDATE SanBong
                SET    TenSan = @TenSan, MaLoai = @MaLoai, GiaTheoGio = @GiaTheoGio,
                       HinhAnh = @HinhAnh, TrangThai = @TrangThai
                WHERE  MaSan = @MaSan";

            ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@MaSan", san.MaSan);
                cmd.Parameters.AddWithValue("@TenSan", san.TenSan);
                cmd.Parameters.AddWithValue("@MaLoai", san.MaLoai);
                cmd.Parameters.AddWithValue("@GiaTheoGio", san.GiaTheoGio);
                cmd.Parameters.AddWithValue("@HinhAnh", (object)san.HinhAnh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", san.TrangThai);
            });
        }

        public void XoaSanBong(int maSan)
        {
            const string sql = "DELETE FROM SanBong WHERE MaSan = @MaSan";
            ExecuteNonQuery(sql, cmd => cmd.Parameters.AddWithValue("@MaSan", maSan));
        }

        // ─────────────────────────────────────────────────────────────
        //  ĐẶT SÂN
        // ─────────────────────────────────────────────────────────────

        public int ThemDatSan(DatSan ds)
        {
            const string sql = @"
                INSERT INTO DatSan (MaND, MaSan, NgayDat, GioBatDau, GioKetThuc, TongTien, GhiChu, TrangThai)
                VALUES (@MaND, @MaSan, @NgayDat, @GioBatDau, @GioKetThuc, @TongTien, @GhiChu, @TrangThai);
                SELECT SCOPE_IDENTITY();";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaND", ds.MaND);
                cmd.Parameters.AddWithValue("@MaSan", ds.MaSan);
                cmd.Parameters.AddWithValue("@NgayDat", ds.NgayDat);
                cmd.Parameters.AddWithValue("@GioBatDau", ds.GioBatDau);
                cmd.Parameters.AddWithValue("@GioKetThuc", ds.GioKetThuc);
                cmd.Parameters.AddWithValue("@TongTien", ds.TongTien);
                cmd.Parameters.AddWithValue("@GhiChu", (object)ds.GhiChu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", ds.TrangThai ?? "Chờ duyệt");
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<DatSan> LayLichSuDatSanTheoNguoiDung(int maND)
        {
            const string sql = @"
                SELECT d.MaDatSan, d.MaND, d.MaSan, d.NgayDat, d.GioBatDau, d.GioKetThuc,
                       d.TongTien, d.GhiChu, d.TrangThai,
                       s.TenSan, s.GiaTheoGio, s.HinhAnh, s.TrangThai AS TrangThaiSan,
                       s.MaLoai, l.TenLoai
                FROM   DatSan d
                JOIN   SanBong s ON d.MaSan = s.MaSan
                JOIN   LoaiSan l ON s.MaLoai = l.MaLoai
                WHERE  d.MaND = @MaND
                ORDER  BY d.NgayDat DESC, d.GioBatDau DESC";

            return QueryList(sql,
                cmd => cmd.Parameters.AddWithValue("@MaND", maND),
                MapDatSan);
        }

        public List<DatSan> LayTatCaDatSan()
        {
            const string sql = @"
                SELECT d.MaDatSan, d.MaND, d.MaSan, d.NgayDat, d.GioBatDau, d.GioKetThuc,
                       d.TongTien, d.GhiChu, d.TrangThai,
                       s.TenSan, s.GiaTheoGio, s.HinhAnh, s.TrangThai AS TrangThaiSan,
                       s.MaLoai, l.TenLoai,
                       nd.HoTen, nd.TaiKhoan
                FROM   DatSan d
                JOIN   SanBong s ON d.MaSan = s.MaSan
                JOIN   LoaiSan l ON s.MaLoai = l.MaLoai
                JOIN   NguoiDung nd ON d.MaND = nd.MaND
                ORDER  BY d.NgayDat DESC, d.GioBatDau DESC";

            return QueryList(sql, null, r =>
            {
                var ds = MapDatSan(r);
                ds.NguoiDung = new NguoiDung
                {
                    HoTen = r["HoTen"].ToString(),
                    TaiKhoan = r["TaiKhoan"].ToString()
                };
                return ds;
            });
        }

        public DatSan LayDatSanTheoMa(int maDatSan)
        {
            const string sql = @"
                SELECT d.MaDatSan, d.MaND, d.MaSan, d.NgayDat, d.GioBatDau, d.GioKetThuc,
                       d.TongTien, d.GhiChu, d.TrangThai,
                       s.TenSan, s.GiaTheoGio, s.HinhAnh, s.TrangThai AS TrangThaiSan,
                       s.MaLoai, l.TenLoai
                FROM   DatSan d
                JOIN   SanBong s ON d.MaSan = s.MaSan
                JOIN   LoaiSan l ON s.MaLoai = l.MaLoai
                WHERE  d.MaDatSan = @MaDatSan";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaDatSan", maDatSan);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) return MapDatSan(r);
                }
            }
            return null;
        }

        /// <summary>Cập nhật trạng thái đơn đặt sân</summary>
        public void CapNhatTrangThaiDatSan(int maDatSan, string trangThai)
        {
            const string sql = "UPDATE DatSan SET TrangThai = @TrangThai WHERE MaDatSan = @MaDatSan";
            ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai);
                cmd.Parameters.AddWithValue("@MaDatSan", maDatSan);
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  THỐNG KÊ / DASHBOARD
        // ─────────────────────────────────────────────────────────────

        public int DemTongDonDat() =>
            (int)ExecuteScalar("SELECT COUNT(1) FROM DatSan");

        public decimal TinhTongDoanhThu() =>
            (decimal)(ExecuteScalar("SELECT ISNULL(SUM(TongTien),0) FROM DatSan WHERE TrangThai = N'Đã duyệt'") ?? 0m);

        public int DemDonDatThang(int thang, int nam) =>
            (int)ExecuteScalar(
                "SELECT COUNT(1) FROM DatSan WHERE MONTH(NgayDat)=@Thang AND YEAR(NgayDat)=@Nam",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Thang", thang);
                    cmd.Parameters.AddWithValue("@Nam", nam);
                });

        public decimal TinhDoanhThuThang(int thang, int nam) =>
            (decimal)(ExecuteScalar(
                "SELECT ISNULL(SUM(TongTien),0) FROM DatSan WHERE TrangThai=N'Đã duyệt' AND MONTH(NgayDat)=@Thang AND YEAR(NgayDat)=@Nam",
                cmd =>
                {
                    cmd.Parameters.AddWithValue("@Thang", thang);
                    cmd.Parameters.AddWithValue("@Nam", nam);
                }) ?? 0m);

        // ─────────────────────────────────────────────────────────────
        //  CHỦ SÂN
        // ─────────────────────────────────────────────────────────────

        /// <summary>Lấy danh sách người dùng theo VaiTro (vd: "Chủ sân", "Khách hàng")</summary>
        public List<NguoiDung> LayDanhSachNguoiDungTheoVaiTro(string vaiTro)
        {
            const string sql = @"
                SELECT MaND, HoTen, TaiKhoan, MatKhau, SoDienThoai, VaiTro
                FROM   NguoiDung
                WHERE  VaiTro = @VaiTro
                ORDER  BY MaND";

            return QueryList(sql,
                cmd => cmd.Parameters.AddWithValue("@VaiTro", vaiTro),
                MapNguoiDung);
        }

        /// <summary>Cập nhật VaiTro của một người dùng</summary>
        public void CapNhatVaiTro(int maND, string vaiTro)
        {
            const string sql = "UPDATE NguoiDung SET VaiTro = @VaiTro WHERE MaND = @MaND";
            ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@VaiTro", vaiTro);
                cmd.Parameters.AddWithValue("@MaND", maND);
            });
        }

        /// <summary>Gán MaChuSan cho sân bóng (phân công chủ sân quản lý sân)</summary>
        public void PhanCongSanChoChuSan(int maND, int maSan)
        {
            const string sql = "UPDATE SanBong SET MaChuSan = @MaND WHERE MaSan = @MaSan";
            ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@MaND", maND);
                cmd.Parameters.AddWithValue("@MaSan", maSan);
            });
        }

        /// <summary>Gỡ chủ sân khỏi tất cả sân đang phân công</summary>
        public void GoChuSanKhoiTatCaSan(int maND)
        {
            const string sql = "UPDATE SanBong SET MaChuSan = NULL WHERE MaChuSan = @MaND";
            ExecuteNonQuery(sql, cmd => cmd.Parameters.AddWithValue("@MaND", maND));
        }

        /// <summary>Lấy map MaND → danh sách TenSan đang phân công cho chủ sân</summary>
        public Dictionary<int, List<string>> LaySanCuaTungChuSan()
        {
            const string sql = @"
                SELECT MaChuSan, TenSan
                FROM   SanBong
                WHERE  MaChuSan IS NOT NULL
                ORDER  BY MaChuSan, TenSan";

            var result = new Dictionary<int, List<string>>();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int maND = (int)r["MaChuSan"];
                        string ten = r["TenSan"].ToString();
                        if (!result.ContainsKey(maND))
                            result[maND] = new List<string>();
                        result[maND].Add(ten);
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        private List<T> QueryList<T>(string sql, Action<SqlCommand> paramSetter, Func<SqlDataReader, T> mapper)
        {
            var list = new List<T>();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(mapper(r));
            }
            return list;
        }

        private void ExecuteNonQuery(string sql, Action<SqlCommand> paramSetter)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(string sql, Action<SqlCommand> paramSetter = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  MAPPERS
        // ─────────────────────────────────────────────────────────────

        private static NguoiDung MapNguoiDung(SqlDataReader r)
        {
            var nd = new NguoiDung();
            nd.MaND = (int)r["MaND"];
            nd.HoTen = r["HoTen"].ToString();
            nd.TaiKhoan = r["TaiKhoan"].ToString();
            nd.MatKhau = r["MatKhau"].ToString();
            nd.SoDienThoai = r["SoDienThoai"] == DBNull.Value ? null : r["SoDienThoai"].ToString();
            nd.VaiTro = r["VaiTro"] == DBNull.Value ? "Khách hàng" : r["VaiTro"].ToString();
            return nd;
        }

        private static SanBong MapSanBong(SqlDataReader r)
        {
            var san = new SanBong();
            san.MaSan = (int)r["MaSan"];
            san.TenSan = r["TenSan"].ToString();
            san.MaLoai = (int?)r["MaLoai"];
            san.GiaTheoGio = (decimal)r["GiaTheoGio"];
            san.HinhAnh = r["HinhAnh"] == DBNull.Value ? null : r["HinhAnh"].ToString();
            san.TrangThai = r["TrangThai"] == DBNull.Value ? "Hoạt động" : r["TrangThai"].ToString();
            // Gán LoaiSan tạm (không dùng EntityRef của LINQ to SQL)
            // Lưu TenLoai vào field bổ sung thông qua ViewBag hoặc ViewModel riêng
            return san;
        }

        private static DatSan MapDatSan(SqlDataReader r)
        {
            var ds = new DatSan();
            ds.MaDatSan = (int)r["MaDatSan"];
            ds.MaND = (int?)r["MaND"];
            ds.MaSan = (int?)r["MaSan"];
            ds.NgayDat = (DateTime)r["NgayDat"];
            ds.GioBatDau = (TimeSpan)r["GioBatDau"];
            ds.GioKetThuc = (TimeSpan)r["GioKetThuc"];
            ds.TongTien = r["TongTien"] == DBNull.Value ? (decimal?)null : (decimal)r["TongTien"];
            ds.GhiChu = r["GhiChu"] == DBNull.Value ? null : r["GhiChu"].ToString();
            ds.TrangThai = r["TrangThai"] == DBNull.Value ? "Chờ duyệt" : r["TrangThai"].ToString();
            return ds;
        }
    }
}
