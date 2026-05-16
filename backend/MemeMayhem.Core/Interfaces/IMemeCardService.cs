using MemeMayhem.Core.Entities;

namespace MemeMayhem.Core.Interfaces;

public interface IMemeCardService
{
    Task SyncImgflipDeckAsync();
}
