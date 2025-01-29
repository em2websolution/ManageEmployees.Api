using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ManageEmployees.Domain.Entities;

namespace ManageEmployees.Infra.Data.EntityConfig
{
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {

        private const string TABLE_NAME = "RefreshTokens";
        private const string TOKEN_COLUMN_NAME = "Token";
        private const string USER_ID_COLUMN_NAME = "UserId";
        private const int USER_ID_MAX_LENGTH = 36;

        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable(TABLE_NAME);

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.Token).HasColumnName(TOKEN_COLUMN_NAME).IsRequired();
            builder.Property(x => x.UserId).HasColumnName(USER_ID_COLUMN_NAME).HasMaxLength(USER_ID_MAX_LENGTH).IsRequired();
        }
    }
}
