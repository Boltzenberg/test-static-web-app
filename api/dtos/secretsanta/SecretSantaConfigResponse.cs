namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record SecretSantaConfigResponse(
        string VersionToken,
        List<PersonDto> People,
        List<RestrictionDto> Restrictions
    );
}
