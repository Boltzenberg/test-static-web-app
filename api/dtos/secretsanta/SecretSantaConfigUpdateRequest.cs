namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record SecretSantaConfigUpdateRequest(
        string VersionToken,
        List<PersonDto> People,
        List<RestrictionDto> Restrictions
    );
}
