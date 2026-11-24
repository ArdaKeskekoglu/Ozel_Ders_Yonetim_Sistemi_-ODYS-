using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Services;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.UnitTests;

public class StudentUpdateAndArchiveServiceTests
{
    private static ApplicationDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpdateStudentAsync_UpdatesFieldsAndSetsUpdatedAtUtcForOwnerTeacher()
    {
        await using var context = CreateDbContext(nameof(UpdateStudentAsync_UpdatesFieldsAndSetsUpdatedAtUtcForOwnerTeacher));
        var service = new StudentService(context);

        var created = await service.CreateStudentAsync(
            "teacher-1",
            new CreateStudentViewModel
            {
                FirstName = "Emir",
                LastName = "Yıldırım",
                PhoneNumber = "0500 111 22 33",
                Notes = "İlk not"
            });
        Assert.NotNull(created.Student);

        var editVm = await service.GetStudentForEditAsync("teacher-1", created.Student.Id);
        Assert.NotNull(editVm);
        Assert.Equal("Emir", editVm.FirstName);

        var updateResult = await service.UpdateStudentAsync(
            "teacher-1",
            new EditStudentViewModel
            {
                Id = created.Student.Id,
                FirstName = "Emirhan",
                LastName = "Yıldırım",
                PhoneNumber = "0533 999 00 11",
                Email = "emirhan@odys.local",
                Notes = "Güncellenmiş AYT notu"
            });

        Assert.True(updateResult.Succeeded);
        Assert.NotNull(updateResult.Student);
        Assert.Equal("Emirhan Yıldırım", updateResult.Student.FullName);
        Assert.Equal("0533 999 00 11", updateResult.Student.PhoneNumber);
        Assert.Equal("emirhan@odys.local", updateResult.Student.Email);
        Assert.Equal("Güncellenmiş AYT notu", updateResult.Student.Notes);
        Assert.NotNull(updateResult.Student.UpdatedAtUtc);
    }

    [Fact]
    public async Task SetStudentArchiveStatusAsync_ArchivesAndUnarchivesWithoutDeletingStudentRecord()
    {
        await using var context = CreateDbContext(nameof(SetStudentArchiveStatusAsync_ArchivesAndUnarchivesWithoutDeletingStudentRecord));
        var service = new StudentService(context);

        var created = await service.CreateStudentAsync(
            "teacher-1",
            new CreateStudentViewModel
            {
                FirstName = "Sude",
                LastName = "Çetin"
            });
        Assert.NotNull(created.Student);
        var studentId = created.Student.Id;

        // 1) Arşivle (FR-016)
        var archiveResult = await service.SetStudentArchiveStatusAsync("teacher-1", studentId, isArchived: true);
        Assert.True(archiveResult.Succeeded);
        Assert.NotNull(archiveResult.Student);
        Assert.True(archiveResult.Student.IsArchived);

        // Kayıt veritabanından silinmemeli, geçmiş bağlantılar için korunmalı
        var persistedInDb = await context.Students.FindAsync(studentId);
        Assert.NotNull(persistedInDb);
        Assert.True(persistedInDb.IsArchived);

        // 2) Yeniden aktif et (FR-017)
        var unarchiveResult = await service.SetStudentArchiveStatusAsync("teacher-1", studentId, isArchived: false);
        Assert.True(unarchiveResult.Succeeded);
        Assert.NotNull(unarchiveResult.Student);
        Assert.False(unarchiveResult.Student.IsArchived);
    }

    [Fact]
    public async Task UpdateAndArchive_ByDifferentTeacher_AreRejectedAndLeaveStudentUnchanged()
    {
        await using var context = CreateDbContext(nameof(UpdateAndArchive_ByDifferentTeacher_AreRejectedAndLeaveStudentUnchanged));
        var service = new StudentService(context);

        var created = await service.CreateStudentAsync(
            "teacher-owner",
            new CreateStudentViewModel
            {
                FirstName = "Kuzey",
                LastName = "Tekinoğlu"
            });
        Assert.NotNull(created.Student);
        var studentId = created.Student.Id;

        // Başka öğretmen düzenleme modelini okuyamamalı
        var unauthorizedGet = await service.GetStudentForEditAsync("teacher-other", studentId);
        Assert.Null(unauthorizedGet);

        // Başka öğretmen öğrenciyi güncelleyememeli
        var unauthorizedUpdate = await service.UpdateStudentAsync(
            "teacher-other",
            new EditStudentViewModel
            {
                Id = studentId,
                FirstName = "Sahte",
                LastName = "İsim"
            });
        Assert.False(unauthorizedUpdate.Succeeded);

        // Başka öğretmen öğrenciyi arşivleyememeli
        var unauthorizedArchive = await service.SetStudentArchiveStatusAsync("teacher-other", studentId, isArchived: true);
        Assert.False(unauthorizedArchive.Succeeded);

        var original = await context.Students.FindAsync(studentId);
        Assert.NotNull(original);
        Assert.Equal("Kuzey", original.FirstName);
        Assert.False(original.IsArchived);
    }
}
