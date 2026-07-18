using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.DTOs;
using UserService.Application.Abstractions;
namespace UserService.Application.Query.GetUsers
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
    {
        private readonly IIdentityProviderService _identityProvider;

        public GetUsersHandler(IIdentityProviderService identityProvider)
        {
            _identityProvider = identityProvider;
        }

        public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            return await _identityProvider.GetUsersAsync(request.SearchTerm, request.Role, cancellationToken);
        }
    }
}
