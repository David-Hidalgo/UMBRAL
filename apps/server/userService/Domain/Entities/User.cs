using System;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Entities
{
    public class User
    {
        private static readonly string[] AllowedRoles = { "admin", "operator", "player" };

        public Guid Id { get; private set; }
        public string KeycloakUserId { get; private set; } = string.Empty;
        public EmailAddress Email { get; private set; } = null!;
        public FullName Name { get; private set; } = null!;
        public string Role { get; private set; } = "player";

        private User() { }

        public bool IsActive { get; private set; } = true;
        public DateTime? DeactivatedAtUtc { get; private set; }

        public static User Create(Guid id, string keycloakUserId, string email, string name, string role = "player")
        {
            if (id == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(keycloakUserId))
                throw new ArgumentException("Keycloak user ID cannot be empty.", nameof(keycloakUserId));

            return new User
            {
                Id = id,
                KeycloakUserId = keycloakUserId,
                Email = EmailAddress.Create(email),
                Name = FullName.Create(name),
                Role = EnsureValidRole(role)
            };
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new InvalidOperationException("User is already inactive.");
            IsActive = false;
            DeactivatedAtUtc = DateTime.UtcNow;
        }

        public void UpdateProfile(string name, string role)
        {
            var newName = FullName.Create(name);
            var validRole = EnsureValidRole(role);
            Name = newName;
            Role = validRole;
        }

        private static string EnsureValidRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role) || Array.IndexOf(AllowedRoles, role) < 0)
                throw new ArgumentException("Role must be 'admin', 'operator', or 'player'.", nameof(role));

            return role;
        }
    }
}
