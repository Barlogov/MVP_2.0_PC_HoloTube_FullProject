using System.Collections;
using System.Collections.Generic;

public class Subscribers_DB
{
    enum Devices
    {
        PC_FullBody,
        PC_Hologram,
        MX_Glusses,
        Phone
    }

    struct User
    {
        uint id;
        Devices device;
        string ipAdr;

        public User(uint new_id, Devices new_device, string new_ipAdr)
        {
            id = new_id;
            device = new_device;
            ipAdr= new_ipAdr;
        }
    }

    List<User> Users = new List<User>();

    public void SetTestDB()
    {
        User testuser = new User(1, Devices.PC_FullBody, "192.168.31.0");

        Users.Add(testuser);
    }
}
