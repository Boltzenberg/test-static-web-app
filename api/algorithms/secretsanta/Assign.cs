using Boltzenberg.Functions.DataModels.SecretSanta;

namespace Boltzenberg.Functions.Algorithms.SecretSanta
{
    public static class Assign
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

            var assignments = AssignSantasBasedOnPermutations.PermuteAndFilter(filters, people);

            foreach (var participant in currentEvent.Participants)
            {
                var assignment = config.People.Find(p => p.Email == assignments[participant.Email]);
                if (assignment == null)
                {
                    return false;
                }

                participant.SantaForName = assignment.Name;
                participant.SantaForEmail = assignment.Email;
            }

            return true;
        }

        private static Dictionary<string, List<string>> GetPeopleToCandidatesUsingConfiguredConstraintsAndNotSameAsLastYear(SecretSantaEvent currentEvent, List<SecretSantaEvent> events, SecretSantaConfig config)
        {
            SecretSantaEvent? previousEvent = events.Find(e => e.GroupName == currentEvent.GroupName && e.Year == currentEvent.Year - 1);

            Dictionary<string, List<string>> peopleToCandidates = new Dictionary<string, List<string>>();
            foreach (var participant in currentEvent.Participants)
            {
                peopleToCandidates[participant.Email] = new List<string>();
                foreach (var candidate in currentEvent.Participants)
                {
                    if (candidate == participant)
                    {
                        continue;
                    }

                    if (config.Restrictions.Find(r => r.Person1Email == candidate.Email && r.Person2Email == participant.Email) != null)
                    {
                        continue;
                    }

                    if (config.Restrictions.Find(r => r.Person1Email == participant.Email && r.Person2Email == candidate.Email) != null)
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

        public static class AssignSantasBasedOnPermutations
        {
            private static Random _random = new Random();

            public static Dictionary<string, string> PermuteAndFilter(Dictionary<string, List<string>> filter, List<string> allPeople)
            {
                List<Dictionary<string, string>> validPermutations = AllValidGivenFilter(filter, allPeople);
                Dictionary<string, string> result = validPermutations[_random.Next(validPermutations.Count)];
                return result;
            }

            public static List<Dictionary<string, string>> AllValidGivenFilter(Dictionary<string, List<string>> filter, List<string> allPeople)
            {
                List<Dictionary<string, string>> validPermutations = new List<Dictionary<string, string>>();

                List<List<string>> allPermutations = new List<List<string>>(Tools.Permutations.Permute(allPeople));

                // 1. Generate All Permutations
                foreach (var perm in allPermutations)
                {
                    // 2. Filter out the bad ones
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

            private static bool IsValidPermutation(Dictionary<string, string> candidate, Dictionary<string, List<string>> filter)
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
}