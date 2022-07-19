using Communications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.SqlClient;
using System.Diagnostics;

namespace StarterCode
{
    /// <summary>
    /// Author:   H. James de St. Germain (starter code), expanded upon by: Greyson Mitra and Abhiveer Sharma
    /// Date:     Spring 2020
    /// Updated:  Spring 2022
    /// 
    /// Code for a simple web server. Takes requests and displays information about Agario knockoff game. Uses database to store and retrieve information
    /// about highscores, etc.
    /// </summary>
    class WebServer
    {
        /// <summary>
        /// keep track of how many requests have come in.  Just used
        /// for display purposes.
        /// </summary>
        static private int counter = 1;

        /// <summary>
        /// The information necessary for the program to connect to the Database
        /// </summary>
        public static readonly string connectionString;

        /// <summary>
        /// Upon construction of this static class, build the connection string
        /// </summary>
        static WebServer()
        {
            var builder = new ConfigurationBuilder();

            builder.AddUserSecrets<WebServer>();
            IConfigurationRoot Configuration = builder.Build();
            //var mySecrets = Configuartion.GetSection("WebServerSecrets");

            connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = Configuration["ServerName"],
                InitialCatalog = Configuration["DBName"],
                UserID = Configuration["UserName"],
                Password = Configuration["DBPassword"]
            }.ConnectionString;
        }


        /// <summary>
        /// Basic connect handler - i.e., a browser has connected!
        /// Print an information message
        /// </summary>
        /// <param name="channel"> the Networking connection</param>
        internal static void onClientConnect(Networking channel)
        {
            //throw new NotImplementedException("Print something about a connection happening");
            Debug.WriteLine($"Hello {channel.RemoteAddressPort}");
            //channel.ClientAwaitMessagesAsync();
        }

        /// <summary>
        /// Create the HTTP response header, containing items such as
        /// the "HTTP/1.1 200 OK" line.
        /// 
        /// See: https://www.tutorialspoint.com/http/http_responses.htm
        /// 
        /// Warning, don't forget that there have to be new lines at the
        /// end of this message!
        /// </summary>
        /// <param name="length"> how big a message are we sending</param>
        /// <param name="type"> usually html, but could be css</param>
        /// <returns> returns a string with the response header </returns>
        private static string BuildHTTPResponseHeader(int length, string type = "text/html")
        {
            string builtHeader = "HTTP/1.1 200 OK\n" + "Connection: close\n" + "Content-Type: text/html; charset=UTF-8\n";

            return $@"
HTTP/1.1 200 OK
Content-Length: {length}
Content-Type: {type}
Connection:Closed" + "\n\n";

        }

        /// <summary>
        ///   Create a web page!  The body of the returned message is the web page
        ///   "code" itself. Usually this would start with the doctype tag followed by the HTML element.  Take a look at:
        ///   https://www.sitepoint.com/a-basic-html5-template/
        /// </summary>
        /// <returns> A string that represents a web page. </returns>
        private static string BuildHTTPBody()
        {
            return $@"
<!doctype html>
<html lang=""en"">
<head>
<title> Cool Agario 2 Website </title>
<meta name = 'description' content = 'Agario stats for nerds.'>
<meta name = 'author' content = 'Greyson Mitra & Abhiveer Sharma'>
<meta property = 'og:title' content = 'Agario 2 title'>
<meta property = 'og:type' content = 'website'>
<meta property = 'og:url' content = 'https://agario2stats.com'>
<meta property = 'og:description' content = 'Agario 2 stats content idk' > 
<link rel = 'stylesheet' href = 'css/styles.css?v=1.0'>
</head>
<h1>Agario 2 Statistics</h1>
<p>You are visitor {counter}!</p>
<a href='localhost:11001'>Reload</a>
<a href='/highscores'>Highscores</a>
<a href='/create'>Generate a new </a>
<br/>how are you...
</body>
</html>";
        }

        /// <summary>
        /// Create a response message string to send back to the connecting
        /// program (i.e., the web browser).  The string is of the form:
        /// 
        ///   HTTP Header
        ///   [new line]
        ///   HTTP Body
        ///  
        ///  The Header must follow the header protocol.
        ///  The body should follow the HTML doc protocol.
        /// </summary>
        /// <returns> the complete HTTP response </returns>
        private static string BuildMainPage()
        {
            string message = BuildHTTPBody();
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        private static string BuildHighscoresPage()
        {
            string table = BuildHighscoresTable();
            string header = BuildHTTPResponseHeader(table.Length);

            return header + table;
        }


        /// <summary>
        ///   <para>
        ///     When a request comes in (from a browser) this method will
        ///     be called by the Networking code.  Each line of the HTTP request
        ///     will come as a separate message.  The "line" we are interested in
        ///     is a PUT or GET request.  
        ///   </para>
        ///   <para>
        ///     The following messages are actionable:
        ///   </para>
        ///   <para>
        ///      get highscore - respond with a highscore page
        ///   </para>
        ///   <para>
        ///      get favicon - don't do anything (we don't support this)
        ///   </para>
        ///   <para>
        ///      get scores - along with a name, respond with a list of scores for the particular user
        ///   </para>
        ///   <para>
        ///     create - contact the DB and create the required tables and seed them with some dummy data
        ///   </para>
        ///   <para>
        ///     get index (or "", or "/") - send a happy home page back
        ///   </para>
        ///   <para>
        ///     get css/styles.css?v=1.0  - send your sites css file data back
        ///   </para>
        ///   <para>
        ///     otherwise send a page not found error
        ///   </para>
        ///   <para>
        ///     Warning: when you send a response, the web browser is going to expect the message to
        ///     be line by line (new line separated) but we use new line as a special character in our
        ///     networking object.  Thus, you have to send _every line of your response_ as a new Send message.
        ///   </para>
        /// </summary>
        /// <param name="network_message_state"> provided by the Networking code, contains socket and message</param>
        internal static void onMessage(Networking channel, string message)
        {
            try
            {
                if (message.StartsWith("GET")) //"GET / HTTP/1.1"
                {
                    string messageRequest = message.Substring(4);
                    if (messageRequest.StartsWith("/highscores"))
                    {
                        string[] responseArr = BuildHighscoresPage().Split("\n");
                        foreach (string line in responseArr)
                        {
                            channel.Send(line);
                        }
                        //When user clicks on links on pages, navigate to webpage and display different information from DB etc.
                    }
                    else if (messageRequest.StartsWith("/scores"))
                    {

                    }
                    else if (messageRequest.StartsWith("/scores/[name]/[highmass]/[highrank]/[starttime]/[endtime]"))
                    {
                        //use this to avoid SSMS and insert stuff into DB
                    }
                    else if (messageRequest.StartsWith("/custom")) // We can decide something for this.
                    {
                        //Something with a cool JS chart
                    }
                    else if (messageRequest == "/ HTTP/1.1")
                    {
                        string[] responseArr = BuildMainPage().Split("\n");
                        foreach (string line in responseArr)
                        {
                            channel.Send(line);
                        }
                        counter++;
                    }
                    else if (messageRequest.StartsWith("/create"))
                    {
                        CreateDBTablesPage();
                    }
                    else if (messageRequest.StartsWith("/css/styles.css?v=1.0"))
                    {

                    }

                    else
                    {
                        //display page not found
                    }
                }
                else if (message.StartsWith("PUT"))
                {
                    //return webpage saying data saved successfully in DB
                }

                channel.Disconnect();
            }
            catch (Exception ex)
            {

            }
        }

        private static string BuildHighscoresTable()
        {
            //List<float> massList = new List<float>();
            //List<float> lifeTimeList = new List<float>();
            //List<string> nameList = new List<string>();
            //List<int> highRankList = new List<int>();
            //List<DateTime> startTimeList = new List<DateTime>();

            //nameList[0] = "Jim";
            //massList[0] = 100.2F;
            //highRankList[0] = 1;
            //lifeTimeList[0] = 1.2F;
            //startTimeList[0] = DateTime.Now;
            //nameList[1] = "Jim";
            //massList[1] = 100.2F;
            //highRankList[1] = 1;
            //lifeTimeList[1] = 1.2F;
            //startTimeList[1] = DateTime.Now;
            //nameList[2] = "Jim";
            //massList[2] = 100.2F;
            //highRankList[2] = 1;
            //lifeTimeList[2] = 1.2F;
            //startTimeList[2] = DateTime.Now;


            //try
            //{
            //    //create instance of database connection
            //    using (SqlConnection con = new SqlConnection(connectionString))
            //    {
            //        con.Open(); //open connection to DB

            //        // This code uses an SqlCommand based on the SqlConnection:
            //        using (SqlCommand command = new SqlCommand("SELECT * FROM Highscores", con))
            //        {
            //            using (SqlDataReader reader = command.ExecuteReader())
            //            {
            //                while (reader.Read())
            //                {
            //                    // If types are known for each column:
            //                    massList.Add(reader.GetFloat(0));
            //                    lifeTimeList.Add(reader.GetFloat(1));
            //                    nameList.Add(reader.GetString(2));
            //                    highRankList.Add(reader.GetInt32(3));
            //                    startTimeList.Add(reader.GetDateTime(4));

            //                    // If column names are known:
            //                    //float.TryParse(reader["Mass"].ToString(), out float massCol);
            //                    //name = reader["Name"].ToString() ?? "";
            //                }
            //            }
            //        }
            //    }
            //    Console.WriteLine($"Successful SQL connection");
            //}
            //catch (SqlException exception)
            //{
            //    Console.WriteLine($"Error in SQL connection: {exception.Message}");
            //}

            return $@"
<!doctype html>
<html lang=""en"">
<head>
<title> Cool Agario 2 Website Highscores </title>
<meta name = 'description' content = 'Agario stats for nerds.'>
<meta name = 'author' content = 'Greyson Mitra & Abhiveer Sharma'>
<meta property = 'og:title' content = 'Agario 2 title'>
<meta property = 'og:type' content = 'website'>
<meta property = 'og:url' content = 'https://agario2stats/highscores.com'>
<meta property = 'og:description' content = 'Agario 2 stats content idk'>
<link rel = 'stylesheet' href = 'css/styles.css?v=1.0'>
</head>
<h1>Agario 2 High Scores</h1>
<a href='localhost:11001'>Back to Main page</a>
<a href='/highscores'>Reload</a>
<br/>how are you...
</body>
</html>";
        }

        //<table>
        //</table>


        //<thead>
        //<tr>
        //<th>Name</th>
        //<th>Mass</th>
        //<th>Rank</th>
        //<th>Lifetime</th>
        //<th>Start time</th>
        //</tr>
        //</thead>
        //<tbody>
        //</tbody>


        //        <tr>
        //<td>{nameList[0]
        //    }</td>
        //<td>{massList[0]
        //}</ td >
        //< td >{ highRankList[0]}</ td >
        //< td >{ lifeTimeList[0]}</ td >
        //< td >{ startTimeList[0]}</ td >
        //</ tr >
        //< tr >
        //< td >{ nameList[1]}</ td >
        //< td >{ massList[1]}</ td >
        //< td >{ highRankList[1]}</ td >
        //  < td >{ lifeTimeList[1]}</ td >
        //     < td >{ startTimeList[1]}</ td >
        //        </ tr >
        //        < tr >
        //        < td >{ nameList[2]}</ td >
        //           < td >{ massList[2]}</ td >
        //              < td >{ highRankList[2]}</ td >
        //                 < td >{ lifeTimeList[2]}</ td >
        //                    < td >{ startTimeList[2]}</ td >
        //                       </ tr >

        /// <summary>
        /// Handle some CSS to make our pages beautiful
        /// </summary>
        /// <returns>HTTP Response Header with CSS file contents added</returns>
        private static string SendCSSResponse()
        {
            throw new NotSupportedException("read the css file from the solution folder, build an http response, and return this string");
            //Note: for starters, simply return a static hand written css string from right here (don't do file reading)
        }


        /// <summary>
        ///    (1) Instruct the DB to seed itself (build tables, add data)
        ///    (2) Report to the web browser on the success
        /// </summary>
        /// <returns> the HTTP response header followed by some informative information</returns>
        private static string CreateDBTablesPage()
        {
            throw new NotImplementedException("create the database tables by 'talking' with the DB server and then return an informative web page");

            // /create protocol used here

            try
            {
                //create instance of database connection
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open(); //open connection to DB

                    // This code uses an SqlCommand based on the SqlConnection:
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Highscores", con))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1}",
                                    reader.GetInt32(0), reader.GetString(1));
                            }
                        }
                    }
                }
                Console.WriteLine($"Successful SQL connection");
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
        }

        internal static void onDisconnect(Networking channel)
        {
            Debug.WriteLine($"Goodbye {channel.RemoteAddressPort}");
        }

    }
}

