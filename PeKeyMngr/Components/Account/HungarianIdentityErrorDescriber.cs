using Microsoft.AspNetCore.Identity;

namespace PeKeyMngr.Components.Account
{
    public sealed class HungarianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            Error(nameof(DefaultError), "Ismeretlen hiba történt.");

        public override IdentityError ConcurrencyFailure() =>
            Error(nameof(ConcurrencyFailure), "Az adatok egyidejű módosítása miatt a művelet nem sikerült.");

        public override IdentityError PasswordMismatch() =>
            Error(nameof(PasswordMismatch), "A megadott jelszó helytelen.");

        public override IdentityError InvalidToken() =>
            Error(nameof(InvalidToken), "A megadott biztonsági kód vagy hivatkozás érvénytelen vagy lejárt.");

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            Error(nameof(RecoveryCodeRedemptionFailed), "A helyreállítási kód érvénytelen vagy már felhasználták.");

        public override IdentityError LoginAlreadyAssociated() =>
            Error(nameof(LoginAlreadyAssociated), "Ez a külső bejelentkezés már egy másik fiókhoz van társítva.");

        public override IdentityError InvalidUserName(string? userName) =>
            Error(nameof(InvalidUserName), $"A(z) '{userName}' felhasználónév érvénytelen.");

        public override IdentityError InvalidEmail(string? email) =>
            Error(nameof(InvalidEmail), $"A(z) '{email}' e-mail-cím érvénytelen.");

        public override IdentityError DuplicateUserName(string userName) =>
            Error(nameof(DuplicateUserName), $"A(z) '{userName}' felhasználónév már foglalt.");

        public override IdentityError DuplicateEmail(string email) =>
            Error(nameof(DuplicateEmail), $"A(z) '{email}' e-mail-cím már egy fiókhoz van rendelve.");

        public override IdentityError InvalidRoleName(string? role) =>
            Error(nameof(InvalidRoleName), $"A(z) '{role}' szerepkörnév érvénytelen.");

        public override IdentityError DuplicateRoleName(string role) =>
            Error(nameof(DuplicateRoleName), $"A(z) '{role}' szerepkör már létezik.");

        public override IdentityError UserAlreadyHasPassword() =>
            Error(nameof(UserAlreadyHasPassword), "A felhasználónak már van jelszava.");

        public override IdentityError UserLockoutNotEnabled() =>
            Error(nameof(UserLockoutNotEnabled), "A felhasználói fiók zárolása nincs engedélyezve.");

        public override IdentityError UserAlreadyInRole(string role) =>
            Error(nameof(UserAlreadyInRole), $"A felhasználó már tagja a(z) '{role}' szerepkörnek.");

        public override IdentityError UserNotInRole(string role) =>
            Error(nameof(UserNotInRole), $"A felhasználó nem tagja a(z) '{role}' szerepkörnek.");

        public override IdentityError PasswordTooShort(int length) =>
            Error(nameof(PasswordTooShort), $"A jelszónak legalább {length} karakter hosszúnak kell lennie.");

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            Error(nameof(PasswordRequiresUniqueChars), $"A jelszónak legalább {uniqueChars} különböző karaktert kell tartalmaznia.");

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            Error(nameof(PasswordRequiresNonAlphanumeric), "A jelszónak legalább egy nem alfanumerikus karaktert kell tartalmaznia.");

        public override IdentityError PasswordRequiresDigit() =>
            Error(nameof(PasswordRequiresDigit), "A jelszónak legalább egy számjegyet kell tartalmaznia.");

        public override IdentityError PasswordRequiresLower() =>
            Error(nameof(PasswordRequiresLower), "A jelszónak legalább egy kisbetűt kell tartalmaznia.");

        public override IdentityError PasswordRequiresUpper() =>
            Error(nameof(PasswordRequiresUpper), "A jelszónak legalább egy nagybetűt kell tartalmaznia.");

        private static IdentityError Error(string code, string description) =>
            new() { Code = code, Description = description };
    }
}
