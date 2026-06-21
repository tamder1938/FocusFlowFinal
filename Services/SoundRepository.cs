using FocusFlowFinal.Models.Sound;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public class SoundRepository : ISoundRepository
{
    private readonly IDatabaseService _db;
    public SoundRepository(IDatabaseService db) => _db = db;

    public IEnumerable<UserSound> GetAll()          => _db.GetAllUserSounds();
    public int  Upsert(UserSound sound)             => _db.UpsertUserSound(sound);
    public void Delete(int id)                      => _db.DeleteUserSound(id);
}
