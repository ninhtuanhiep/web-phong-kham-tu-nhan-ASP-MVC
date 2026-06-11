using BCrypt.Net;

namespace web_phong_kham_tu_nhan.Helpers
{
    /// <summary>
    /// Helper tập trung xử lý mật khẩu — dùng BCrypt để hash và verify.
    /// Tất cả code liên quan đến password đều gọi qua class này.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash mật khẩu plain text thành BCrypt hash để lưu vào DB.
        /// </summary>
        public static string Hash(string plainPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
        }

        /// <summary>
        /// So sánh mật khẩu người dùng nhập với hash trong DB.
        /// Trả về true nếu khớp.
        /// </summary>
        public static bool Verify(string plainPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(plainPassword) || string.IsNullOrEmpty(hashedPassword))
                return false;

            // ✅ Tương thích ngược: nếu hash chưa phải BCrypt (tài khoản cũ plain text),
            // trả về false để buộc user dùng "Quên mật khẩu"
            if (!hashedPassword.StartsWith("$2"))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tạo mật khẩu ngẫu nhiên 8 ký tự (ít nhất 1 hoa, 1 số, 1 ký tự đặc biệt).
        /// </summary>
        public static string GenerateRandom()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "@#!";

            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);

            var result = new System.Text.StringBuilder();
            result.Append(chars[bytes[0] % chars.Length]);
            result.Append(digits[bytes[1] % digits.Length]);
            result.Append(special[bytes[2] % special.Length]);
            for (int i = 3; i < 8; i++)
                result.Append((chars + digits)[bytes[i] % (chars.Length + digits.Length)]);

            // Xáo trộn
            var arr = result.ToString().ToCharArray();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = bytes[i % bytes.Length] % (i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return new string(arr);
        }
    }
}
