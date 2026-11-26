using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Zoo.DAL.Entities;
using Zoo.DAL.Enums;

namespace Zoo.DAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ZooDbContext db, Func<AdminUser, string, string> hashPassword)
    {
        await db.Database.MigrateAsync();

        // 1. Seed Admin mặc định (admin / Admin@123)
        AdminUser admin;
        if (!await db.AdminUsers.AnyAsync())
        {
            admin = new AdminUser
            {
                Username = "admin",
                Role = "Admin",
                IsActive = true
            };
            admin.PasswordHash = hashPassword(admin, "Admin@123");
            db.AdminUsers.Add(admin);
            await db.SaveChangesAsync();
        }
        else
        {
            admin = await db.AdminUsers.FirstAsync();
        }

        // 2. Seed 6 điểm thực tế tại Thảo Cầm Viên + bản Published 5 ngôn ngữ + map sẵn audio trong storage/audio
        if (!await db.Places.AnyAsync())
        {
            var now = DateTime.UtcNow;

            var seedItems = new[]
            {
                new SeedPlaceData(
                    PlaceId: Guid.Parse("152a698e-600d-48fe-a1bf-439a7c801a4b"),
                    RevisionId: Guid.Parse("5e2a9568-b234-4805-8917-bbe1d9a1cd55"),
                    Code: "MAIN_GATE",
                    Category: "facility",
                    Latitude: 10.787348,
                    Longitude: 106.705056,
                    RadiusMeters: 30,
                    MapX: 120,
                    MapY: 420,
                    Priority: 1,
                    ImageUrl: "/images/entrance.jpg",
                    Translations: new[]
                    {
                        ("vi", "Cổng chính Thảo Cầm Viên", "Cổng lịch sử số 2 Nguyễn Bỉnh Khiêm, điểm khởi đầu hành trình khám phá.", "Chào mừng quý khách đến với Thảo Cầm Viên Sài Gòn, một trong những thảo cầm viên lâu đời nhất thế giới được xây dựng từ năm 1864.", "audio/152a698e-600d-48fe-a1bf-439a7c801a4b/5e2a9568-b234-4805-8917-bbe1d9a1cd55/vi-464dc9d90a0b474ab3e2bd193c9fa7db.mp3"),
                        ("en", "Saigon Zoo Main Gate", "Historic gate at 2 Nguyen Binh Khiem Street, the starting point of your journey.", "Welcome to Saigon Zoo and Botanical Gardens, one of the oldest zoos in the world, founded in 1864.", "audio/152a698e-600d-48fe-a1bf-439a7c801a4b/5e2a9568-b234-4805-8917-bbe1d9a1cd55/en-30e627aa832e4d7aaae010299d2b638b.mp3"),
                        ("zh", "西贡动植物园正门", "位于阮秉谦街2号的历史大门，旅程的起点。", "欢迎来到西贡动植物园，它建于1864年，是世界上最古老的动物园之一。", "audio/152a698e-600d-48fe-a1bf-439a7c801a4b/5e2a9568-b234-4805-8917-bbe1d9a1cd55/zh-b50132f611ed449d9617c0c7ea54e906.mp3"),
                        ("ko", "사이공 동물원 정문", "응우옌 빈 키엠 거리 2번지의 역사적인 정문입니다.", "1864년에 설립된 세계에서 가장 오래된 동물원 중 하나인 사이공 동식물원에 오신 것을 환영합니다.", "audio/152a698e-600d-48fe-a1bf-439a7c801a4b/5e2a9568-b234-4805-8917-bbe1d9a1cd55/ko-4515b4df54c54a38a21e7eef8f18a7da.mp3"),
                        ("ja", "サイゴン動植物園 正門", "グエンビンキエム通り2番地にある歴史的な正門です。", "1864年に設立された世界で最も古い動物園の一つ、サイゴン動植物園へようこそ。", "audio/152a698e-600d-48fe-a1bf-439a7c801a4b/5e2a9568-b234-4805-8917-bbe1d9a1cd55/ja-621945bae9b24fbabedc9b8c5b843f47.mp3")
                    }
                ),
                new SeedPlaceData(
                    PlaceId: Guid.Parse("489ecc8b-059c-40ef-8f81-0c7726c6af0c"),
                    RevisionId: Guid.Parse("686c6c13-c250-4f3b-9332-1bbfa75fc666"),
                    Code: "ASIAN_ELEPHANT",
                    Category: "animal",
                    Latitude: 10.788909,
                    Longitude: 106.707196,
                    RadiusMeters: 30,
                    MapX: 280,
                    MapY: 310,
                    Priority: 2,
                    ImageUrl: "/images/elephant.jpg",
                    Translations: new[]
                    {
                        ("vi", "Khu Voi châu Á", "Không gian sinh sống bán tự nhiên của đàn voi châu Á.", "Trước mắt quý khách là khu vực sinh sống của đàn voi châu Á, loài động vật trên cạn lớn nhất Đông Nam Á với trí nhớ tuyệt vời và đời sống bầy đàn gắn kết.", "audio/489ecc8b-059c-40ef-8f81-0c7726c6af0c/686c6c13-c250-4f3b-9332-1bbfa75fc666/vi-65d46c39d8144f5486693880c112ef5b.mp3"),
                        ("en", "Asian Elephant Zone", "Semi-natural habitat of the Asian elephant herd.", "Before you is the habitat of the Asian elephants, the largest land animals in Southeast Asia, known for their intelligence and strong family bonds.", "audio/489ecc8b-059c-40ef-8f81-0c7726c6af0c/686c6c13-c250-4f3b-9332-1bbfa75fc666/en-d98716c69d8a480498181266d5097b6b.mp3"),
                        ("zh", "亚洲象区", "亚洲象群的半自然栖息空间。", "眼前是亚洲象的栖息区，它们是东南亚最大的陆地动物，以出色的记忆力和紧密的群体生活著称。", "audio/489ecc8b-059c-40ef-8f81-0c7726c6af0c/686c6c13-c250-4f3b-9332-1bbfa75fc666/zh-b8ab2bf3e06444ad8779451655b45cf1.mp3"),
                        ("ko", "아시아 코끼리 구역", "아시아 코끼리 무리의 반자연 서식지입니다.", "눈앞에 보이는 곳은 동남아시아에서 가장 큰 육상 동물인 아시아 코끼리의 서식지입니다.", "audio/489ecc8b-059c-40ef-8f81-0c7726c6af0c/686c6c13-c250-4f3b-9332-1bbfa75fc666/ko-d80435f90e784b52a82cb44c750be590.mp3"),
                        ("ja", "アジアゾウエリア", "アジアゾウの群れが暮らす半自然の空間です。", "目の前は東南アジア最大の陸上動物であるアジアゾウのエリアです。優れた記憶力と強い群れの絆を持っています。", "audio/489ecc8b-059c-40ef-8f81-0c7726c6af0c/686c6c13-c250-4f3b-9332-1bbfa75fc666/ja-4a07352ef3e24685847d39537008bae0.mp3")
                    }
                ),
                new SeedPlaceData(
                    PlaceId: Guid.Parse("749316a7-98a8-45ea-bbd8-3de3456fccac"),
                    RevisionId: Guid.Parse("893ac790-673b-4b5a-a093-f75541e19a62"),
                    Code: "GIRAFFE_ZONE",
                    Category: "animal",
                    Latitude: 10.788336,
                    Longitude: 106.707489,
                    RadiusMeters: 30,
                    MapX: 360,
                    MapY: 240,
                    Priority: 2,
                    ImageUrl: "/images/giraffe.jpg",
                    Translations: new[]
                    {
                        ("vi", "Khu Hươu cao cổ", "Khu chuồng mở nơi du khách có thể quan sát gần loài thú cao nhất thế giới.", "Chào mừng quý khách đến với khu hươu cao cổ. Với chiều cao hơn năm mét, chiếc cổ dài giúp chúng dễ dàng vươn tới những tán lá non trên cao.", "audio/749316a7-98a8-45ea-bbd8-3de3456fccac/893ac790-673b-4b5a-a093-f75541e19a62/vi-63a187da066a4b7fbe7c536aece3df7f.mp3"),
                        ("en", "Giraffe Zone", "Open enclosure to observe the tallest land mammals up close.", "Welcome to the giraffe enclosure. Reaching over five meters tall, their long necks allow them to browse tender leaves high in the treetops.", "audio/749316a7-98a8-45ea-bbd8-3de3456fccac/893ac790-673b-4b5a-a093-f75541e19a62/en-9acfbbbd708b488da28bfa30aa888180.mp3"),
                        ("zh", "长颈鹿区", "可近距离观赏世界最高陆地动物的开放园区。", "欢迎来到长颈鹿区。它们身高超过五米，修长的脖子使其能够轻松吃到树冠上的嫩叶。", "audio/749316a7-98a8-45ea-bbd8-3de3456fccac/893ac790-673b-4b5a-a093-f75541e19a62/zh-32a309ee7df5446aab92cdbe7e5c3701.mp3"),
                        ("ko", "기린 구역", "세계에서 가장 키가 큰 동물을 가까이서 볼 수 있는 곳입니다.", "기린 구역에 오신 것을 환영합니다. 5미터가 넘는 키와 긴 목 덕분에 높은 나무의 어린잎을 쉽게 먹을 수 있습니다.", "audio/749316a7-98a8-45ea-bbd8-3de3456fccac/893ac790-673b-4b5a-a093-f75541e19a62/ko-2a4056cb080b4a9794d7bd4e1509d321.mp3"),
                        ("ja", "キリンエリア", "世界で最も背の高い動物を間近で観察できるエリアです。", "キリンエリアへようこそ。高さ5メートル以上にもなる長い首で、高い木の若葉を楽に食べることができます。", "audio/749316a7-98a8-45ea-bbd8-3de3456fccac/893ac790-673b-4b5a-a093-f75541e19a62/ja-1320a595845c40daac7a139176404363.mp3")
                    }
                ),
                new SeedPlaceData(
                    PlaceId: Guid.Parse("ee4e37b5-8fd3-44bf-8380-2b3afc2ff971"),
                    RevisionId: Guid.Parse("b9fcb210-a626-449c-ad4d-109df608185c"),
                    Code: "ANCIENT_TREE",
                    Category: "plant",
                    Latitude: 10.787814,
                    Longitude: 106.707834,
                    RadiusMeters: 25,
                    MapX: 210,
                    MapY: 350,
                    Priority: 1,
                    ImageUrl: "/images/ancient-tree.jpg",
                    Translations: new[]
                    {
                        ("vi", "Khu Cây cổ thụ trăm tuổi", "Bộ sưu tập thực vật nhiệt đới với nhiều cây đại thụ trên 100 năm tuổi.", "Thảo Cầm Viên còn là vườn thực vật đồ sộ với hàng trăm cây cổ thụ như dầu rái, xà cừ và giáng hương tạo nên không gian xanh mát giữa lòng thành phố.", "audio/ee4e37b5-8fd3-44bf-8380-2b3afc2ff971/b9fcb210-a626-449c-ad4d-109df608185c/vi-19c62d451e3744428340a90ba347dc52.mp3"),
                        ("en", "Century-Old Trees Zone", "Tropical botanical collection featuring trees over 100 years old.", "Saigon Zoo is also a vast botanical garden with hundreds of century-old trees providing cool shade in the heart of the city.", "audio/ee4e37b5-8fd3-44bf-8380-2b3afc2ff971/b9fcb210-a626-449c-ad4d-109df608185c/en-155f2b9f31c54237bc44dd8837a4a9d5.mp3"),
                        ("zh", "百年古树区", "拥有众多百年以上热带古树的植物区。", "西贡动植物园也是一座庞大的植物园，拥有数百棵百年古树，为市中心带来一片清凉绿洲。", "audio/ee4e37b5-8fd3-44bf-8380-2b3afc2ff971/b9fcb210-a626-449c-ad4d-109df608185c/zh-4803635a110840f392cdcc9d5eb01a5e.mp3"),
                        ("ko", "백년 고목 구역", "100년 이상 된 열대 고목들이 모여 있는 식물 구역입니다.", "사이공 동식물원은 도심 속에 시원한 그늘을 만들어 주는 수백 그루의 백년 고목을 보유한 거대한 식물원이기도 합니다.", "audio/ee4e37b5-8fd3-44bf-8380-2b3afc2ff971/b9fcb210-a626-449c-ad4d-109df608185c/ko-e3dd75a430864e958cc8973c4eaa9b1e.mp3"),
                        ("ja", "百年古木エリア", "樹齢100年を超える熱帯の古木が集まる植物エリアです。", "サイゴン動植物園は、都会の中心に涼しい木陰を作る数百本の古木を有する壮大な植物園でもあります。", "audio/ee4e37b5-8fd3-44bf-8380-2b3afc2ff971/b9fcb210-a626-449c-ad4d-109df608185c/ja-b8c7a48d506d430aae71f9bd7a9aa0c7.mp3")
                    }
                ),
                new SeedPlaceData(
                    PlaceId: Guid.Parse("ef85bc9e-221c-44b8-81cf-d148e5cc3662"),
                    RevisionId: Guid.Parse("8fd4f7eb-8846-4c12-b0eb-992bca75d66b"),
                    Code: "LION_TIGER_ZONE",
                    Category: "animal",
                    Latitude: 10.787078,
                    Longitude: 106.707633,
                    RadiusMeters: 35,
                    MapX: 390,
                    MapY: 280,
                    Priority: 2,
                    ImageUrl: "/images/lion.jpg",
                    Translations: new[]
                    {
                        ("vi", "Khu Sư tử & Hổ Bengal", "Khu bảo tồn các loài thú họ Mèo lớn với hào nước và vách kính an toàn.", "Đây là khu vực bảo tồn sư tử châu Phi và hổ trắng Bengal quý hiếm, giúp du khách quan sát tập tính săn mồi và sinh hoạt của các loài mèo lớn.", "audio/ef85bc9e-221c-44b8-81cf-d148e5cc3662/8fd4f7eb-8846-4c12-b0eb-992bca75d66b/vi-7d6d8b099ebe46fab1d331bddee8be0a.mp3"),
                        ("en", "Lion & Bengal Tiger Zone", "Big cat conservation area with moats and safe viewing glass.", "This area houses African lions and rare white Bengal tigers, offering visitors a close look at the behavior of these majestic big cats.", "audio/ef85bc9e-221c-44b8-81cf-d148e5cc3662/8fd4f7eb-8846-4c12-b0eb-992bca75d66b/en-25515aa64f194acb96a2418a0c0783d4.mp3"),
                        ("zh", "狮子与孟加拉虎区", "配有护城河和安全玻璃的大型猫科动物保护区。", "这里是保护非洲狮和珍稀白色孟加拉虎的区域，让游客近距离观察大型猫科动物的习性。", "audio/ef85bc9e-221c-44b8-81cf-d148e5cc3662/8fd4f7eb-8846-4c12-b0eb-992bca75d66b/zh-5dd7c8a9d3af46d9a2f1b01dc6e8325b.mp3"),
                        ("ko", "사자 & 벵골 호랑이 구역", "안전 유리와 해자가 있는 대형 고양잇과 동물 보호 구역입니다.", "이곳은 아프리카 사자와 희귀한 백색 벵골 호랑이를 보호하고 있는 구역입니다.", "audio/ef85bc9e-221c-44b8-81cf-d148e5cc3662/8fd4f7eb-8846-4c12-b0eb-992bca75d66b/ko-7ca310e9ffd34088b3a89814c59f060f.mp3"),
                        ("ja", "ライオン・ベンガルトラエリア", "安全なガラスと堀を備えた大型ネコ科動物の保護エリアです。", "ここはアフリカライオンや希少なホワイトタイガーを保護しているエリアです。", "audio/ef85bc9e-221c-44b8-81cf-d148e5cc3662/8fd4f7eb-8846-4c12-b0eb-992bca75d66b/ja-1b73e6a3b6424c2b96bb6dd00c617ecc.mp3")
                    }
                ),
                new SeedPlaceData(
                    PlaceId: Guid.Parse("f1968968-65c0-4971-a233-b16a0ed0ed21"),
                    RevisionId: Guid.Parse("c72bb150-ef34-4f63-b375-9b37c0d289b6"),
                    Code: "LOTUS_POND",
                    Category: "plant",
                    Latitude: 10.787632,
                    Longitude: 106.706504,
                    RadiusMeters: 30,
                    MapX: 250,
                    MapY: 390,
                    Priority: 1,
                    ImageUrl: "/images/lotus.jpg",
                    Translations: new[]
                    {
                        ("vi", "Hồ Sen & Vườn Bướm", "Không gian mặt nước thanh bình kết hợp vườn hoa thu hút bướm nhiệt đới.", "Quý khách đang dừng chân tại Hồ Sen và Vườn Bướm, không gian thư thái với mặt hồ sen hồng cùng nhiều loài bướm bản địa rực rỡ.", "audio/f1968968-65c0-4971-a233-b16a0ed0ed21/c72bb150-ef34-4f63-b375-9b37c0d289b6/vi-7bb6864c3eba4e87bffac397a3035ade.mp3"),
                        ("en", "Lotus Pond & Butterfly Garden", "Peaceful lakeside space paired with a tropical butterfly flower garden.", "You are stopping at the Lotus Pond and Butterfly Garden, a tranquil spot filled with blooming pink lotuses and colorful native butterflies.", "audio/f1968968-65c0-4971-a233-b16a0ed0ed21/c72bb150-ef34-4f63-b375-9b37c0d289b6/en-8a1b70497398482dac2aed6751f6e881.mp3"),
                        ("zh", "莲花池与蝴蝶园", "宁静的水面空间与吸引热带蝴蝶的花园。", "您现在正停留在莲花池与蝴蝶园，这里荷花盛开，吸引着众多色彩斑斓的本土蝴蝶。", "audio/f1968968-65c0-4971-a233-b16a0ed0ed21/c72bb150-ef34-4f63-b375-9b37c0d289b6/zh-3570b490f8564bfc89bb554a914c0bf9.mp3"),
                        ("ko", "연꽃 연못 & 나비 정원", "평화로운 연못과 열대 나비가 모이는 꽃 정원입니다.", "분홍빛 연꽃과 다채로운 토종 나비들이 어우러진 평화로운 휴식 공간입니다.", "audio/f1968968-65c0-4971-a233-b16a0ed0ed21/c72bb150-ef34-4f63-b375-9b37c0d289b6/ko-ea270fd735b14bc0bdc276a3f8673dc3.mp3"),
                        ("ja", "蓮池とチョウの庭園", "穏やかな水辺と熱帯のチョウが集まる花園です。", "ピンク色の蓮が咲く池と、色鮮やかな在来のチョウを楽しめる穏やかな空間です。", "audio/f1968968-65c0-4971-a233-b16a0ed0ed21/c72bb150-ef34-4f63-b375-9b37c0d289b6/ja-d9ebd1c278604b168b717c06b21aa6e2.mp3")
                    }
                )
            };

            // Bước 2a: Lưu Places trước (PublishedRevisionId = null để tránh lỗi vòng lặp khóa ngoại)
            var places = seedItems.Select(item => new Place
            {
                Id = item.PlaceId,
                Code = item.Code,
                Category = item.Category,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                RadiusMeters = item.RadiusMeters,
                MapX = item.MapX,
                MapY = item.MapY,
                Priority = item.Priority,
                ImageUrl = item.ImageUrl,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            db.Places.AddRange(places);
            await db.SaveChangesAsync();

            // Bước 2b: Lưu Revisions & 5 bản dịch mỗi điểm
            var revisions = seedItems.Select(item => new NarrationRevision
            {
                Id = item.RevisionId,
                PlaceId = item.PlaceId,
                Version = 1,
                Status = RevisionStatus.Published,
                CreatedByAdminId = admin.Id,
                CreatedAt = now,
                PublishedAt = now,
                Translations = item.Translations.Select(t =>
                {
                    var hash = ComputeSha256(t.Script);
                    return new NarrationTranslation
                    {
                        Id = Guid.NewGuid(),
                        RevisionId = item.RevisionId,
                        Language = t.Lang,
                        Title = t.Title,
                        Description = t.Desc,
                        ScriptText = t.Script,
                        ContentHash = hash,
                        AudioPath = t.AudioPath,
                        AudioContentHash = hash,
                        AudioStatus = AudioStatus.Ready,
                        IsEditedByAdmin = false,
                        UpdatedAt = now
                    };
                }).ToList()
            }).ToList();

            db.NarrationRevisions.AddRange(revisions);
            await db.SaveChangesAsync();

            // Bước 2c: Gắn PublishedRevisionId cho từng Place
            foreach (var item in seedItems)
            {
                var place = places.First(p => p.Id == item.PlaceId);
                place.PublishedRevisionId = item.RevisionId;
            }
            await db.SaveChangesAsync();
        }

        // 3. Seed nhãn giao diện cơ bản cho 5 ngôn ngữ
        if (!await db.SiteTexts.AnyAsync())
        {
            db.SiteTexts.AddRange(
                new SiteText { Key = "app.title", Language = "vi", Value = "Thuyết Minh Tự Động Thảo Cầm Viên" },
                new SiteText { Key = "app.title", Language = "en", Value = "Saigon Zoo Audio Guide" },
                new SiteText { Key = "app.title", Language = "zh", Value = "西贡动植物园语音导览" },
                new SiteText { Key = "app.title", Language = "ko", Value = "사이공 동물원 오디오 가이드" },
                new SiteText { Key = "app.title", Language = "ja", Value = "サイゴン動植物園オーディオガイド" }
            );
            await db.SaveChangesAsync();
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record SeedPlaceData(
        Guid PlaceId,
        Guid RevisionId,
        string Code,
        string Category,
        double Latitude,
        double Longitude,
        double RadiusMeters,
        double MapX,
        double MapY,
        int Priority,
        string ImageUrl,
        (string Lang, string Title, string Desc, string Script, string AudioPath)[] Translations
    );
}