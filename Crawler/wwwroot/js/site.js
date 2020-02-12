window.jQuery || document.write('<script src="/docs/4.4/assets/js/vendor/jquery.slim.min.js"><\/script>');



var app = angular.module('crawlerapp', []);
app.controller('CrawlerController', function ($scope, $http) {

    $scope.crawlurls = [];
    $scope.PostFormDatas = [];
    $scope.FinishLoading = false;
    $scope.Loading = false;
    $scope.Depth = 0;
    $scope.LoadTitle = function (titleurl, index) {

        var data = {
            url: titleurl
        };
        $http.post('https://localhost:44380/api/Crawler/GetSingleTitleAsync', data).then(function (response) {
            if (response.data)
                $scope.crawlurls[index] = response.data;
        }, function (response) {

        });
    };


    $scope.PostForm = function (titleurl) {

        var data = {
            url: titleurl
        };
        $http.post('https://localhost:44380/api/Crawler/PostFormAsync', data).then(function (response) {
            if (response.data) {
                $scope.PostFormDatas = response.data;
                console.log($scope.PostFormDatas);

                //$('#ShowPostLink').modal('show');
            }

        }, function (response) {

        });
    };

    $scope.StartCrawling = function () {
        $scope.FinishLoading = false;
        $scope.Loading = true;

        var data = {
            url: $scope.enterurl,
            depth: 0,
            loadFromSiteMap: 2,
            isHttps: false

        };
        if ($scope.LoadSiteMap == true) {
            data.loadFromSiteMap = 1;
        }
        if ($scope.UseHttps == true) {
            data.isHttps =true;
        }

        data.depth = $scope.Depth;

        


        $http.post('https://localhost:44380/api/Crawler', data).then(function (response) {

            if (response.data) {
                $scope.crawlurls = response.data;
                $scope.FinishLoading = true;
                $scope.Loading = false;
            }
            
        }, function (response) {
            console.log(response.statusText);
            $scope.FinishLoading = false;
            $scope.Loading = false;
            // $scope.msg = "Service not Exists";

            // $scope.statusval = response.status;

            // $scope.statustext = response.statusText;

            // $scope.headers = response.headers();

        });


    };
});