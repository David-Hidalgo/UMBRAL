using System;
using UserService.Domain.ValueObjects;

namespace UserService.Domain.Entities
{
    public class User
    {
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
                Role = role
            };
        }
    }
}
