﻿module.exports = ['$http', 'apiUrl',
    function ($http, apiUrl) {

        var controllerUrl = apiUrl + 'IPSpec/';

        // GET
        // Get all IPSpecs.
        function getSpecs() {

            // Make request.
            return $http.get(controllerUrl + 'GetAll').then(
                function (response) {

                    // Return unpacked data.
                    return response.data;
                });
        }

        // POST
        // Create a new IPSpec.
        function createSpec(name, ipAddress) {

            // Make request.
            return $http.post(controllerUrl + `Create?name=${name}&ip=${ipAddress}`).then(
                function (response) {
                    return {
                        err: null,
                        ipSpec: response.data
                    }
                },
                function (response) {
                    return {
                        err: response.data.Message
                    }
                });
        }

        // DELETE
        // Delete IPSpec;
        function deleteSpec(ipSpec) {

            // Make request.
            return $http.delete(controllerUrl + 'Delete?id=' + ipSpec.IPSpecId).then(
                function (response) {

                    // Return unpacked data.
                    return response.data;
                });
        }

        return {
            getSpecs: getSpecs,
            createSpec: createSpec,
            deleteSpec: deleteSpec
        };

    }];