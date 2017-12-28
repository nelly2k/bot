using System.Threading.Tasks;
using bot.model;

namespace bot.core
{
    public interface INotSoldRepository: IService
    {
        Task SetNotSold(string platform, string pair, bool isBorrowed);
    }

    public class NotSoldRepository: BaseRepository, INotSoldRepository
    {

        public async Task SetNotSold(string platform, string pair, bool isBorrowed)
        {
            await Execute(async cmd =>
            {
                cmd.CommandText =
                    @"update balance 
                      set notSoldCounter = notSoldCounter + 1, notSoldDate = getdate()
                      where platform=@platform and name=@pair and isDeleted=0 and isBorrowed=@isBorrowed";

                cmd.Parameters.AddWithValue("@platform", platform);
                cmd.Parameters.AddWithValue("@pair", pair);
                cmd.Parameters.AddWithValue("@isBorrowed", isBorrowed);

                await cmd.ExecuteNonQueryAsync();
            });
        }

    }
}
