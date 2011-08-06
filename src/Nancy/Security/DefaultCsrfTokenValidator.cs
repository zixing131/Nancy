namespace Nancy.Security
{
    using System;
    using System.Linq;
    using Cryptography;

    public class DefaultCsrfTokenValidator : ICsrfTokenValidator
    {
        private readonly IHmacProvider hmacProvider;

        public DefaultCsrfTokenValidator(IHmacProvider hmacProvider)
        {
            this.hmacProvider = hmacProvider;
        }

        /// <summary>
        /// Validates a pair of tokens
        /// </summary>
        /// <param name="tokenOne">First token (usually from either a form post or querystring)</param>
        /// <param name="tokenTwo">Second token (usually from a cookie)</param>
        /// <param name="salt">Optional salt value specified during creation</param>
        /// <param name="validityPeriod">Optional period that the tokens are valid for</param>
        /// <returns>Token validation result</returns>
        public CsrfTokenValidationResult Validate(CsrfToken tokenOne, CsrfToken tokenTwo, string salt = null, TimeSpan? validityPeriod = new TimeSpan?())
        {
            if (tokenOne == null || tokenTwo == null)
            {
                return CsrfTokenValidationResult.TokenMissing;
            }

            if (!String.Equals(tokenOne.Salt ?? String.Empty, salt ?? String.Empty, StringComparison.Ordinal))
            {
                return CsrfTokenValidationResult.SaltMismatch;
            }

            if (!tokenOne.Equals(tokenTwo))
            {
                return CsrfTokenValidationResult.TokenMismatch;
            }

            if (tokenOne.RandomBytes == null || tokenOne.RandomBytes.Length == 0)
            {
                return CsrfTokenValidationResult.TokenTamperedWith;
            }

            var newToken = new CsrfToken
                               {
                                   CreatedDate = tokenOne.CreatedDate,
                                   RandomBytes = tokenOne.RandomBytes,
                                   Salt = tokenOne.Salt,
                               };
            newToken.CreateHmac(this.hmacProvider);
            if (!newToken.Hmac.SequenceEqual(tokenOne.Hmac))
            {
                return CsrfTokenValidationResult.TokenTamperedWith;
            }

            if (validityPeriod.HasValue)
            {
                var expiryDate = tokenOne.CreatedDate.Add(validityPeriod.Value);

                if (DateTime.Now > expiryDate)
                {
                    return CsrfTokenValidationResult.TokenExpired;
                }
            }

            return CsrfTokenValidationResult.Ok;
        }
    }
}