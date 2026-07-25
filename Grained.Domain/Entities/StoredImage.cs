namespace Grained.Domain.Entities;

// A binary image (campaign logos for now), served via GET /api/images/{id}. Stored in the DB for
// dev simplicity; swap for object storage (S3/Cloudinary) later without changing callers/URLs.
public class StoredImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
