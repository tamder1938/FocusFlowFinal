using FocusFlowFinal.Models;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public interface IPlaceRepository
{
    List<PlaceItem> GetAll();
    PlaceItem? Get(int id);
    void Upsert(PlaceItem place);
    void Delete(int id);
}
