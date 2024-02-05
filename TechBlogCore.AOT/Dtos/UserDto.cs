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

        public IEnumerable<string> Roles { get; set; }
    }

    public class UserPasswordVerifyDto
    {
        public long Id { get; set; }

        public string Account { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public long? Avatar_Id { get; set; }

        public bool IsDisabled { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsAdmin { get; set; }

        public string Hash { get; set; }

        public string Salt { get; set; }
    }

    public class UserRoleDto
    {
        public long User_Id { get; set; }
        public long Role_Id { get; set; }
    }

    public class UserStatusDto
    {
        public string? role { get; set; }
        public string? user { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
    }
}
