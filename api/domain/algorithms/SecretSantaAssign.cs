using Boltzenberg.Functions.Algorithms.Tools;

namespace Boltzenberg.Functions.Domain.Algorithms
{
    public static class SecretSantaAssign
    {
        public static bool AssignSantas(SecretSantaEvent currentEvent, List<SecretSantaEvent> events, SecretSantaConfig config)
        {
            if (currentEvent.IsRunning)
            {
                return false;
            }

            Dictionary<string, List<string>> filters = GetPeopleToCandidatesUsingConfiguredConstraintsAndNotSameAsLastYear(currentEvent, events, config);
            List<string> people = new List<string>();
            foreach (var participant in currentEvent.Participants)
            {
                people.Add(participant.Email);
            }

            List<Dictionary<string, string>> validPermutations = AllValidGivenFilter(filters, people);
            if (validPermutations.Count == 0)
            {
                return false;
            }

            var random = new Random();
            Dictionary<string, string> assignments = validPermutations[random.Next(validPermutations.Count)];

            // Build new participants with assignments applied
            var updatedParticipants = new List<SecretSantaEvent.Participant>();
            foreach (var participant in currentEvent.Participants)
            {
                var assignedEmail = assignments[participant.Email];
                var assignedPerson = config.People.Find(p => p.Email == assignedEmail);
                if (assignedPerson == null)
                {
                    return false;
                }

                updatedParticipants.Add(new SecretSantaEvent.Participant(
                    participant.Name,
                    participant.Email,
                    assignedPerson.Name,
                    assignedPerson.Email
                ));
            }

            // Replace participants in currentEvent — note: domain SecretSantaEvent uses init-only,
            // so we mutate the list in place (it's the same list reference)
            currentEvent.Participants.Clear();
            currentEvent.Participants.AddRange(updatedParticipants);

            return true;
        }

        private static Dictionary<string, List<string>> GetPeopleToCandidatesUsingConfiguredConstraintsAndNotSameAsLastYear(
            SecretSantaEvent currentEvent,
            List<SecretSantaEvent> events,
            SecretSantaConfig config)
        {
            SecretSantaEvent? previousEvent = events.Find(e =>
                e.GroupName == currentEvent.GroupName && e.Year == currentEvent.Year - 1);

            Dictionary<string, List<string>> peopleToCandidates = new Dictionary<string, List<string>>();
            foreach (var participant in currentEvent.Participants)
            {
                peopleToCandidates[participant.Email] = new List<string>();
                foreach (var candidate in currentEvent.Participants)
                {
                    if (candidate.Email == participant.Email)
                    {
                        continue;
                    }

                    if (config.Restrictions.Find(r =>
                        r.Person1Email == candidate.Email && r.Person2Email == participant.Email) != null)
                    {
                        continue;
                    }

                    if (config.Restrictions.Find(r =>
                        r.Person1Email == participant.Email && r.Person2Email == candidate.Email) != null)
                    {
                        continue;
                    }

                    if (previousEvent != null)
                    {
                        var previousParticipant = previousEvent.Participants.Find(p => p.Email == participant.Email);
                        if (previousParticipant != null && previousParticipant.SantaForEmail == candidate.Email)
                        {
                            continue;
                        }
                    }

                    peopleToCandidates[participant.Email].Add(candidate.Email);
                }
            }

            return peopleToCandidates;
        }

        public static List<Dictionary<string, string>> AllValidGivenFilter(
            Dictionary<string, List<string>> filter,
            List<string> allPeople)
        {
            List<Dictionary<string, string>> validPermutations = new List<Dictionary<string, string>>();
            List<List<string>> allPermutations = new List<List<string>>(Permutations.Permute(allPeople));

            foreach (var perm in allPermutations)
            {
                Dictionary<string, string> candidate = new Dictionary<string, string>();
                for (int i = 0; i < perm.Count; i++)
                {
                    candidate[allPeople[i]] = perm[i];
                }

                if (IsValidPermutation(candidate, filter))
                {
                    validPermutations.Add(candidate);
                }
            }

            return validPermutations;
        }

        private static bool IsValidPermutation(
            Dictionary<string, string> candidate,
            Dictionary<string, List<string>> filter)
        {
            foreach (string personId in candidate.Keys)
            {
                if (!filter[personId].Contains(candidate[personId]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
