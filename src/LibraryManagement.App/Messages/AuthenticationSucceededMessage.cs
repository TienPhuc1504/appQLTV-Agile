using LibraryManagement.Core.Models;

namespace LibraryManagement.App.Messages;

public sealed record AuthenticationSucceededMessage(CurrentUser User);
