<?php
$uri = "https://domain.ddd/";
if(isset($_FILES['imagedata']['name'])) {
        $path = 'i/' . substr(md5(time()), -28) . '.png';
    if(move_uploaded_file($_FILES['imagedata']['tmp_name'], $path)) {
        echo $uri , $path;
    } else{
        echo $uri;
    }
} else {
    echo $uri;
} ?>