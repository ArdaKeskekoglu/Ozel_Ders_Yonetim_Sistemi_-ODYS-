using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Subjects;

namespace OzelDers.UnitTests;

public class SubjectServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateSubjectAsync_FirstSubjectBecomesDefaultAutomatically_AndSecondCanBeOptionalDefault()
    {
        await using var context = CreateDbContext(nameof(CreateSubjectAsync_FirstSubjectBecomesDefaultAutomatically_AndSecondCanBeOptionalDefault));
        var service = new SubjectService(context);

        // İlk ders SetAsDefault = false gönderilse bile tek aktif ders olduğundan otomatik varsayılan olmalı
        var first = await service.CreateSubjectAsync(
            "teacher-1",
            new CreateSubjectViewModel
            {
                Name = "  Matematik ",
                SetAsDefault = false
            });

        Assert.True(first.Succeeded);
        Assert.NotNull(first.Subject);
        Assert.Equal("Matematik", first.Subject.Name);
        Assert.True(first.Subject.IsDefault);

        // İkinci ders SetAsDefault = false ile eklendiğinde ilk ders varsayılan kalmalı
        var second = await service.CreateSubjectAsync(
            "teacher-1",
            new CreateSubjectViewModel
            {
                Name = "Geometri",
                SetAsDefault = false
            });

        Assert.True(second.Succeeded);
        Assert.NotNull(second.Subject);
        Assert.False(second.Subject.IsDefault);

        // Üçüncü ders SetAsDefault = true ile eklendiğinde yeni varsayılan olmalı ve önceki varsayılan sıfırlanmalı
        var third = await service.CreateSubjectAsync(
            "teacher-1",
            new CreateSubjectViewModel
            {
                Name = "Fizik",
                SetAsDefault = true
            });

        Assert.True(third.Succeeded);
        Assert.NotNull(third.Subject);
        Assert.True(third.Subject.IsDefault);

        var list = await service.GetSubjectsForTeacherAsync("teacher-1");
        Assert.Equal("Fizik", list.DefaultSubjectName);
        Assert.Single(list.Subjects, s => s.IsDefault);
    }

    [Fact]
    public async Task CreateSubjectAsync_RejectsDuplicateActiveName_AndReactivatesMatchingArchivedSubject()
    {
        await using var context = CreateDbContext(nameof(CreateSubjectAsync_RejectsDuplicateActiveName_AndReactivatesMatchingArchivedSubject));
        var service = new SubjectService(context);

        var math = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Matematik" });
        var geo = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Geometri" });
        Assert.NotNull(math.Subject);
        Assert.NotNull(geo.Subject);

        // Aynı isimde aktif ders tekrar eklenememeli
        var duplicateActive = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "matematik" });
        Assert.False(duplicateActive.Succeeded);
        Assert.Contains("Bu isimde aktif bir ders türünüz zaten mevcut.", duplicateActive.Errors);

        // Geometri dersini arşivle
        var archiveGeo = await service.SetSubjectArchiveStatusAsync("teacher-1", geo.Subject.Id, isArchived: true);
        Assert.True(archiveGeo.Succeeded);

        // Arşivdeki Geometri adıyla yeni ders eklenmek istendiğinde mükerrer satır açmak yerine arşivdeki kayıt yeniden etkinleştirilmeli
        var reactivateGeo = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Geometri", SetAsDefault = true });
        Assert.True(reactivateGeo.Succeeded);
        Assert.NotNull(reactivateGeo.Subject);
        Assert.Equal(geo.Subject.Id, reactivateGeo.Subject.Id);
        Assert.False(reactivateGeo.Subject.IsArchived);
        Assert.True(reactivateGeo.Subject.IsDefault);
        Assert.Equal(2, await context.Subjects.CountAsync(s => s.TeacherId == "teacher-1"));
    }

    [Fact]
    public async Task SetSubjectArchiveStatusAsync_PreventsArchivingLastActiveSubject_AndPromotesNewDefaultWhenDefaultIsArchived()
    {
        await using var context = CreateDbContext(nameof(SetSubjectArchiveStatusAsync_PreventsArchivingLastActiveSubject_AndPromotesNewDefaultWhenDefaultIsArchived));
        var service = new SubjectService(context);

        var math = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Matematik" });
        Assert.NotNull(math.Subject);

        // Tek aktif ders varken arşivleme reddedilmeli
        var archiveLast = await service.SetSubjectArchiveStatusAsync("teacher-1", math.Subject.Id, isArchived: true);
        Assert.False(archiveLast.Succeeded);
        Assert.Contains(archiveLast.Errors, e => e.Contains("En az bir aktif ders türünüz bulunmalıdır"));

        // İkinci ders eklendikten sonra varsayılan olan Matematik arşivlenirse Geometri otomatik olarak yeni varsayılan olmalı
        var geo = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Geometri" });
        Assert.NotNull(geo.Subject);

        var archiveDefault = await service.SetSubjectArchiveStatusAsync("teacher-1", math.Subject.Id, isArchived: true);
        Assert.True(archiveDefault.Succeeded);

        var activeList = await service.GetSubjectsForTeacherAsync("teacher-1", SubjectStatusFilter.Active);
        var remainingActive = Assert.Single(activeList.Subjects);
        Assert.Equal("Geometri", remainingActive.Name);
        Assert.True(remainingActive.IsDefault);
        Assert.Equal("Geometri", activeList.DefaultSubjectName);
    }

    [Fact]
    public async Task SubjectOperations_IsolateTeachersAndRejectCrossTeacherModifications()
    {
        await using var context = CreateDbContext(nameof(SubjectOperations_IsolateTeachersAndRejectCrossTeacherModifications));
        var service = new SubjectService(context);

        var t1Math = await service.CreateSubjectAsync("teacher-1", new CreateSubjectViewModel { Name = "Matematik" });
        var t2Math = await service.CreateSubjectAsync("teacher-2", new CreateSubjectViewModel { Name = "Matematik" });
        var t2Physics = await service.CreateSubjectAsync("teacher-2", new CreateSubjectViewModel { Name = "Fizik" });

        Assert.True(t1Math.Succeeded);
        Assert.True(t2Math.Succeeded);
        Assert.NotNull(t2Physics.Subject);

        // Teacher-1, Teacher-2'nin dersini düzenleyememeli, varsayılan yapamamalı ve arşivleyememeli
        Assert.Null(await service.GetSubjectForEditAsync("teacher-1", t2Physics.Subject.Id));

        var badUpdate = await service.UpdateSubjectAsync(
            "teacher-1",
            new EditSubjectViewModel { Id = t2Physics.Subject.Id, Name = "Yetkisiz" });
        Assert.False(badUpdate.Succeeded);

        var badDefault = await service.SetDefaultSubjectAsync("teacher-1", t2Physics.Subject.Id);
        Assert.False(badDefault.Succeeded);

        var badArchive = await service.SetSubjectArchiveStatusAsync("teacher-1", t2Physics.Subject.Id, isArchived: true);
        Assert.False(badArchive.Succeeded);

        var t1List = await service.GetSubjectsForTeacherAsync("teacher-1", SubjectStatusFilter.All);
        var singleT1 = Assert.Single(t1List.Subjects);
        Assert.Equal(t1Math.Subject!.Id, singleT1.Id);
    }
}
