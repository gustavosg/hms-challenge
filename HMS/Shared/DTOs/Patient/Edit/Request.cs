using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Patient.Edit;

public sealed record Request(
    Guid Id,
    string Name,
    DateOnly BirthDate,
    string Document,
    string Contact,
    string Email,
    string PhoneNumber
);
