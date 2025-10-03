using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Patient.Get;

public sealed record Request(
    Guid? UserId,
    string? Name,
    string? Document,
    string? Email,
    DateOnly? BirthDateMin,
    DateOnly? BirthDateMax
    );
