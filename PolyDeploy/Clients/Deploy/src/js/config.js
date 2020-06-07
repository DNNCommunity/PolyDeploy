﻿module.exports = ['$stateProvider', '$urlRouterProvider', '$httpProvider',
    function ($stateProvider, $urlRouterProvider, $httpProvider) {

        // Default route.
        $urlRouterProvider.otherwise('/upload');

        // States.
        $stateProvider
            .state('install', {
                template: require('./templates/install.html')
            })
            .state('install.upload', {
                url: '/upload',
                template: require('./templates/upload.html'),
                controller: 'UploadController'
            })
            .state('install.summary', {
                template: require('./templates/summary.html'),
                controller: 'SummaryController'
            })
            .state('install.result', {
                template: require('./templates/result.html'),
                controller: 'ResultController'
            });

        // Add $http interceptor for DNN Services Framework.
        $httpProvider.interceptors.push(['DnnService',
            function (DnnService) {
                return {
                    request: function (config) {

                        var securityHeaders = DnnService.getSecurityHeaders();

                        Object.keys(securityHeaders).forEach(function (key) {
                            config.headers[key] = securityHeaders[key];
                        });

                        return config;
                    }
                };
            }]);
    }];