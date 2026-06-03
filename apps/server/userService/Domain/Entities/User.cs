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

        /// <summary>
        /// Comportamiento de mutacion semantico para HU52. Protege los invariantes:
        /// el nombre se reemplaza por un Value Object <see cref="FullName"/> recien validado
        /// (los VO son inmutables) y el rol se valida contra la lista permitida.
        /// No expone setters publicos.
        /// </summary>
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
