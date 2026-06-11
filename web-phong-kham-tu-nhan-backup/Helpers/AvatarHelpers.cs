public static class AvatarHelper
{
    // Lấy chữ cái đại diện (initials)
    public static string GetInitials(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "?";

        var parts = fullName.Trim().Split(' ');
        return parts.Length > 1
            ? (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper()
            : parts[0][0].ToString().ToUpper();
    }

    // Lấy avatar hiển thị
    public static string GetAvatar(string? imageUrl, string fullName)
    {
        if (!string.IsNullOrEmpty(imageUrl))
        {
            return imageUrl; // bác sĩ có ảnh
        }

        return GetInitials(fullName); // bệnh nhân → lấy chữ
    }
}