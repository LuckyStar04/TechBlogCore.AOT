namespace TechBlogCore.AOT.Dtos
{
    public class UserDto
    {
        public string Id { get; set; }

        public string Account { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Avatar_Id { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsLock { get; set; }

        public DateTime? LockExpire { get; set; }

        public IEnumerable<string> Roles { get; set; }
    }

    public class UserPasswordVerifyDto
    {
        public long Id { get; set; }

        public string Account { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Avatar_Id { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsLock { get; set; }

        public int LockRetry { get; set; }

        public DateTime? LockExpire { get; set; }

        public string Hash { get; set; }

        public string Salt { get; set; }
    }

    public class UserStatusDto
    {
        public string? role { get; set; }
        public string? user { get; set; }
        public string? email { get; set; }
    }
}
