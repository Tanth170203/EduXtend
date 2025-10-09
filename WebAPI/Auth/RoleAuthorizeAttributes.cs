using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Auth
{
    /// <summary>
    /// Base class for role-based authorization attributes
    /// </summary>
    public abstract class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        protected RoleAuthorizeAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// Requires Admin role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AdminOnlyAttribute : RoleAuthorizeAttribute
    {
        public AdminOnlyAttribute() : base("Admin") { }
    }

    /// <summary>
    /// Requires Student role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class StudentOnlyAttribute : RoleAuthorizeAttribute
    {
        public StudentOnlyAttribute() : base("Student") { }
    }

    /// <summary>
    /// Requires ClubManager role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ClubManagerOnlyAttribute : RoleAuthorizeAttribute
    {
        public ClubManagerOnlyAttribute() : base("ClubManager") { }
    }

    /// <summary>
    /// Requires ClubMember role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ClubMemberOnlyAttribute : RoleAuthorizeAttribute
    {
        public ClubMemberOnlyAttribute() : base("ClubMember") { }
    }

    /// <summary>
    /// Requires Admin or ClubManager role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ClubManagementAttribute : RoleAuthorizeAttribute
    {
        public ClubManagementAttribute() : base("Admin", "ClubManager") { }
    }

    /// <summary>
    /// Requires Admin, ClubManager, or ClubMember role
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ClubAccessAttribute : RoleAuthorizeAttribute
    {
        public ClubAccessAttribute() : base("Admin", "ClubManager", "ClubMember") { }
    }

    /// <summary>
    /// Requires any authenticated user (any role)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AllUsersAttribute : RoleAuthorizeAttribute
    {
        public AllUsersAttribute() : base("Admin", "Student", "ClubManager", "ClubMember") { }
    }
}



