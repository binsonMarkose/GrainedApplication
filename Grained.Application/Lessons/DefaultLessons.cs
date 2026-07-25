using Grained.Domain.Entities;
using Grained.Domain.Enums;

namespace Grained.Application.Lessons;

// A starter Nursery lesson library, adapted from the IPC Sunday Schools Association "Sunday School
// Text Book — Nursery 1" (20 lessons). Each lesson is complete and publish-ready: a child-friendly
// story, the moral as the learning objective, a memory verse, a simple activity, and one quiz
// question. Seeded as Published so a church can start teaching immediately; admins can edit, unpublish,
// reassign, delete, or add their own. Source attributed via AuthorName.
public static class DefaultLessons
{
    public const string Author = "IPC Sunday Schools Association";

    public record Def(
        string Title, string BibleReference, string? Theme, string Story, string Moral,
        string VerseText, string VerseRef, string Activity, string? Prayer,
        string Question, string[] Options, int CorrectIndex);

    public static readonly IReadOnlyList<Def> Catalog =
    [
        new("Creation", "Genesis 1:1-9", "Creation",
            "Long, long ago the earth was dark and empty, covered with water. God decided to make it beautiful. God said, “Let there be light,” and there was light. He called the light “day” and the darkness “night.” Then God made the sky. God looked at all He had made and saw that it was good.",
            "God created the beautiful heavens and earth for us.",
            "In the beginning God created the Heavens and the Earth.", "Genesis 1:1",
            "Colour the creation picture and count the flowers.", null,
            "Who created the heavens and the earth?", ["God", "People", "No one"], 0),

        new("Trees", "Genesis 1:9-19", "Creation",
            "On the third day God gathered the waters together and dry land appeared. God called it “Earth.” Then the earth brought forth grass, plants, and trees full of fruit. On the fourth day God made the sun, moon, and stars to give light and to mark day and night. God saw that it was good.",
            "God provided light, fruits, and trees — all for our good.",
            "God called the dry land Earth.", "Genesis 1:10",
            "Colour the trees and count the flowers.", null,
            "What did God call the dry land?", ["Earth", "Sky", "Sea"], 0),

        new("Birds", "Genesis 1:20-26", "Creation",
            "On the fifth day God made a beautiful thing. He filled the seas with fish and made whales and creatures that swim. He made birds to fly in the sky. Then God made the cattle, the animals, and every living creature. God gave us everything we need.",
            "It was God who made all the things we need.",
            "Look at the birds in the air; they neither sow nor reap nor gather into barns.", "Matthew 6:26",
            "Colour the birds and the animals.", null,
            "What did God make to fly in the sky?", ["Birds", "Fish", "Trees"], 0),

        new("God", "Genesis 1; Deuteronomy 26:15; Job 22:12", "God",
            "God made a beautiful world for us to live in — food, air, light, trees, birds, fish, and animals. God is the maker of everything. He is everywhere, He is all-powerful, and He takes care of us. There is only one God, and nothing is too hard for Him.",
            "Almighty God provides for me and protects me.",
            "Is anything too hard for the Lord?", "Genesis 18:14",
            "Colour the picture of God’s throne in heaven.", null,
            "How many true Gods are there?", ["One", "Two", "Many"], 0),

        new("Parents", "Genesis 1:26–2:24", "Family",
            "God made the first man from the dust and breathed life into him. He named him Adam. Then God made Eve to be Adam’s companion. God gave them a beautiful garden, the Garden of Eden, to live in and care for. God gives us parents to love us and care for us.",
            "We should obey what God and our parents tell us.",
            "Children, obey your parents in the Lord.", "Ephesians 6:1",
            "Colour the picture of Adam and Eve.", null,
            "Who were the first man and woman?", ["Adam and Eve", "Cain and Abel", "Noah and Sarah"], 0),

        new("Family", "Genesis 4:1-2; 5:1-5", "Family",
            "God gave children to Adam and Eve. Cain was the first son, then Abel, and later Seth. Family is God’s idea — a father, a mother, and children living together, loving and praying to God. Fathers and mothers work hard to give their children food, clothes, and everything they need.",
            "God wants children to obey their parents, help at home, and love one another.",
            "But as for me and my house, we will serve the Lord.", "Joshua 24:15",
            "Write the names of your family members.", null,
            "Who made the family?", ["God", "The teacher", "The children"], 0),

        new("Love", "John 13:34", "Love",
            "Jesus loved everyone, even Judas who was going to betray Him. Jesus said the greatest love is to give your own life for others. Jesus wants us to love one another just as He loved us, and to share with others.",
            "Love one another as Jesus loved us.",
            "As I have loved you, that you also love one another.", "John 13:34",
            "Colour the picture of Jesus with the children.",
            "Lord, bless me with Your love so that we may love one another.",
            "How does Jesus want us to love one another?", ["As He loved us", "Only our friends", "Not at all"], 0),

        new("Prayer", "Daniel 6", "Prayer",
            "Daniel loved God and prayed to Him three times every day. Some jealous men made a rule that no one could pray to God, or they would be thrown to the lions. But Daniel kept praying. He was thrown into the lions’ den, but God shut the lions’ mouths and kept Daniel safe.",
            "When we keep praying to the Lord, God will save us from every danger.",
            "Pray without ceasing.", "1 Thessalonians 5:17",
            "Colour Daniel in the lions’ den.", null,
            "How many times a day did Daniel pray?", ["Three", "One", "Never"], 0),

        new("Strengthening God", "1 Samuel 17:1-54", "Faith",
            "David was a young shepherd boy. With God’s strength he had protected his sheep from lions and bears. One day David saw the giant Goliath making fun of God’s people. With God’s help, David took a small stone, threw it at Goliath, and the giant fell down. God gave David the victory.",
            "With the power of God we can face every enemy.",
            "I can do all things through Christ who strengthens me.", "Philippians 4:13",
            "Colour the picture of David and Goliath.", null,
            "Who helped David defeat Goliath?", ["God", "The king", "No one"], 0),

        new("Jesus", "Luke 2:1-21; Matthew 1:18-21", "Christmas",
            "An angel told Mary she would have a special baby who would save the people from their sins, and He must be named Jesus. Mary and Joseph went to Bethlehem, and there Jesus was born and laid in a manger. That night angels told the shepherds the good news, and they came to see baby Jesus and were very happy.",
            "The happiest thing in life is knowing Jesus.",
            "For unto you is born a Saviour, Christ the Lord.", "Luke 2:11",
            "Colour the picture of baby Jesus in the manger.", null,
            "Where was baby Jesus laid?", ["In a manger", "In a big house", "On a boat"], 0),

        new("Peace", "Luke 24:36-49; John 14:1-27", "Peace",
            "When we are sad or afraid, we run to our mother and she comforts us. After Jesus was crucified, His disciples were very frightened and hid in a room. But Jesus came to them and said, “My peace I give to you. Do not be afraid.” Jesus gives us His peace.",
            "When we are sad, Jesus gives us peace.",
            "My peace I give unto you.", "John 14:27",
            "Colour the picture of Jesus with His disciples.", null,
            "What did Jesus give His frightened disciples?", ["Peace", "Money", "Food"], 0),

        new("Child Jesus in the Temple", "Luke 2:52", "Jesus",
            "When Jesus was twelve, He went with His parents to the temple in Jerusalem for a feast. After the feast His parents started home, but Jesus stayed behind. They found Him three days later in the temple, listening and talking with the teachers about God’s word. Everyone was amazed at His wisdom.",
            "We should read God’s word and pray every day, and grow up in the Lord.",
            "Jesus increased in wisdom and stature and in favour with God.", "Luke 2:52",
            "Colour the picture of Jesus in the temple.", null,
            "Where did Jesus’ parents find Him?", ["In the temple", "At the market", "By the sea"], 0),

        new("Wedding in Cana", "John 2:1-11", "Miracles",
            "Jesus, His mother, and His disciples were invited to a wedding in Cana. During the feast the wine ran out. Jesus’ mother told the servants to do whatever Jesus said. Jesus told them to fill six stone pots with water. When they poured it out, the water had become the best wine! This was Jesus’ first miracle.",
            "Jesus can make up for anything we lack.",
            "Whatever Jesus says to you, do it.", "John 2:5",
            "Count the water pots and colour the picture.", null,
            "What did Jesus turn the water into?", ["Wine", "Milk", "Juice"], 0),

        new("The Good Boy", "John 6:1-27", "Miracles",
            "A big crowd came to hear Jesus, and they were hungry. There was no bread and no money to buy any. A boy shared his five loaves and two fish with Jesus. Jesus prayed, blessed the food, and shared it. A wonder happened — everyone ate and was full, and there were twelve baskets left over!",
            "When we give what we have to God, it becomes a blessing to many.",
            "Jesus took the loaves, thanked God and distributed them to the people; likewise the fish.", "John 6:11",
            "Count the baskets of leftover bread.", null,
            "What did the boy share with Jesus?", ["Five loaves and two fish", "His toys", "His money"], 0),

        new("The Lost Sheep", "John 10:1-6", "Parables",
            "A shepherd had a hundred sheep. Each night he called them by name into the fold. One night one sheep was missing. The shepherd left the ninety-nine and searched everywhere until he found the lost sheep and carried it safely home. Jesus is our Good Shepherd who loves us like that.",
            "Jesus is the good shepherd who loves the sheep.",
            "I am the good shepherd; the good shepherd gives his life for the sheep.", "John 10:11",
            "Colour the shepherd and the sheep.", null,
            "What did the shepherd do when one sheep was lost?", ["Went to find it", "Went home", "Got a new sheep"], 0),

        new("The Miraculous Fishing", "Luke 5:1-11", "Miracles",
            "Jesus preached by the lake of Gennesaret. Simon and the fishermen had fished all night and caught nothing. Jesus told Simon to go into the deep water and let down the nets. Simon obeyed, and they caught so many fish the nets began to break! They had to call other boats to help.",
            "When we obey God, it becomes a great blessing.",
            "If you believe you will see the glory of God.", "John 11:40",
            "Find the hidden fish and colour them.", null,
            "What happened when Simon obeyed Jesus?", ["They caught many fish", "The boat sank", "Nothing happened"], 0),

        new("Daughter of Jairus", "Luke 8:40-56", "Miracles",
            "Jairus, a leader, came to Jesus because his little daughter was very sick. On the way, someone said she had died. Jesus said, “Fear not, only believe.” Jesus went to the girl, took her hand, and said, “Rise up.” The girl got up as if from sleep! Everyone was amazed, and Jesus told them to give her food.",
            "It is Jesus who gives us life.",
            "Fear not, only believe and she shall be saved.", "Luke 8:50",
            "Colour the picture of Jairus’ daughter.", null,
            "What did Jesus do for Jairus’ daughter?", ["Gave her life", "Gave her toys", "Sent her away"], 0),

        new("Jesus Calms the Storm", "Luke 8:22-25", "Miracles",
            "Jesus and His disciples were crossing the lake in a boat, and Jesus fell asleep. Suddenly a great storm came and water filled the boat. The frightened disciples woke Jesus. Jesus stood up and told the wind and the waves, “Be still!” At once the storm stopped and everything was calm.",
            "God controls everything; when Jesus commands, the wind, water, and rain all obey.",
            "He commands even the winds and water, and they obey him!", "Luke 8:25",
            "Colour the picture of Jesus calming the storm.", null,
            "What obeyed Jesus in the storm?", ["The wind and the waves", "The fish", "The birds"], 0),

        new("Jesus Heals the Blind", "Luke 18:35-43", "Miracles",
            "A blind man was begging by the road in Jericho. When he heard Jesus was passing by, he cried out, “Jesus, Son of David, have mercy on me!” People told him to be quiet, but he shouted louder. Jesus called him and asked what he wanted. “I want to see!” he said. Jesus said, “Receive your sight; your faith has made you well.” At once the man could see.",
            "Keep praying, “Lord, heal me,” and Jesus will heal you.",
            "Receive your sight; your faith has made you well.", "Luke 18:42",
            "Find the hidden eye and colour the picture.", null,
            "What did the blind man want from Jesus?", ["To see", "Money", "Food"], 0),

        new("Jesus Heals the Officer’s Son", "John 4:46-54", "Miracles",
            "An officer’s son in Capernaum was very sick and near death. The officer came to Jesus in Cana and begged Him to heal his son. Jesus said, “Go home; your son is living.” The man believed and went home. On the way his servants met him with good news — his son was well, healed at the very time Jesus spoke!",
            "Those who believe in Jesus are healed. When Jesus speaks, it is done.",
            "Jesus said to him, “Go your way; your son is living.”", "John 4:50",
            "Colour the picture of Jesus and the officer.", null,
            "When was the officer’s son healed?", ["At the time Jesus spoke", "The next week", "Never"], 0),
    ];

    // Fresh, publish-ready Lesson graphs (Lesson + MemoryVerse + Quiz + one Question + Options) for the
    // given church. Not yet added to a context.
    public static IEnumerable<Lesson> ForChurch(Guid churchId, string authorName = Author)
    {
        foreach (var d in Catalog)
        {
            var question = new QuizQuestion { QuestionText = d.Question, Points = 1 };
            for (var i = 0; i < d.Options.Length; i++)
                question.Options.Add(new QuizOption { OptionText = d.Options[i], IsCorrect = i == d.CorrectIndex });

            var quiz = new Quiz { Title = $"{d.Title} Quiz" };
            quiz.Questions.Add(question);

            yield return new Lesson
            {
                ChurchId = churchId,
                Title = d.Title,
                BibleReference = d.BibleReference,
                Theme = d.Theme,
                AgeGroup = "Nursery",
                StoryContent = d.Story,
                LearningObjective = d.Moral,
                Activity = d.Activity,
                Prayer = d.Prayer,
                Status = LessonStatus.Published,
                IsPublished = true,
                AuthorName = authorName,
                MemoryVerse = new MemoryVerse { VerseText = d.VerseText, BibleReference = d.VerseRef },
                Quiz = quiz,
            };
        }
    }
}
