# VR-Game
Table SQL
CREATE TABLE `unitydb`.`player` (
`id` INT(10) NOT NULL AUTO_INCREMENT,
`username` VARCHAR(40) NOT NULL ,
`time` DOUBLE NOT NULL ,
UNIQUE (`id`)) ENGINE = InnoDB;

eaUser.PHP
<?PHP
$dbuser = 'root';
$dbhost = 'localhost';
$dbpass = ''; //type your password here
$db = 'UnityDB'; //type your password here
$username = $_POST["usernamePost"];
$time = $_POST["timePost"];

//make connection
$conn = mysqli_connect($dbhost,$dbuser,$dbpass,$db);
if (!$conn) {
die("Connection failed: " . mysqli_connect_error());
}
//
$query = "INSERT INTO player (username, time) values ('".$username."' , ".$time." ) ";//sql
$result = mysqli_query($conn, $query);
//echo $query;
if(!$result) echo "there was an error";
else echo "everything ok."
?>

eadata.PHP
<?PHP
$dbuser = 'root';
$dbhost = 'localhost';
$dbpass = ''; //type your password here
$db = 'UnityDB'; //type your password here
$conn = mysqli_connect($dbhost,$dbuser,$dbpass,$db);
if (!$conn) {
die("Connection failed: " . mysqli_connect_error());
}
// $query = 'select * from player'; //sql
$query = 'select * from player order by time ASC;'; //sql

$result = mysqli_query($conn, $query);
if ($result) {
while ($row = mysqli_fetch_assoc($result))
{
echo "|Name:".$row["username"]."|time:".$row["time"].";";
}
} else {
die("Error selecting records: " . mysql_error());
}
mysqli_close($conn);
?>

