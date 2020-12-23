// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

function bulkDelete(query) {

    //
    // Stored procedure that bulk soft deletes documents with a given query.
    // Best to make sure the query filters documents that were already soft deleted.
    //
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();
    var response = getContext().getResponse();

    var responseBody = {
        deleted: 0,
        continuation: true
    };

    // Begin run query and delete everything
    processQuery();

    //
    // Recursively runs the query w/ support for continuation tokens.
    //
    function processQuery(continuation) {
        var requestOptions = { continuation: continuation };
        var isAccepted = collection.queryDocuments(collectionLink, query, requestOptions,
            function (err, retrievedDocs, responseOptions) {
                if (err) {
                    throw err;
                }
                if (retrievedDocs.length > 0) {
                    // Begin deleting documents as soon as documents are returned as results.
                    // deleteDocs() resumes querying after deleting everything returned
                    deleteDocs(retrievedDocs);
                }
                else if (responseOptions.continuation) {
                    // If the query came back with continuation token use the token to query.
                    processQuery(responseOptions.continuation);
                }
                else {
                    // We are done.
                    responseBody.continuation = false;
                    response.setBody(responseBody);
                }
            });

        // If we hit execution bounds - return continuation: true.
        if (!isAccepted) {
            response.setBody(responseBody);
        }
    }

    //
    // Recursively deletes documents passed in as an array argument.
    //
    function deleteDocs(documents) {
        while (documents.length > 0) {
            if (documents[0]._isDeleted) {
                // Already deleted shift to next
                responseBody.deleted++;
                documents.shift();
            }
            else {
                // Mark the document with our soft delete flag and replace it.
                documents[0]._isDeleted = true;
                documents[0].ttl = 300;
                var isAccepted = collection.replaceDocument(documents[0]._self, {},
                    function (err, responseOptions) {
                        if (err) {
                            throw err;
                        }

                        responseBody.deleted++;
                        documents.shift();
                        // Delete the next document in the array.
                        deleteDocs(documents);
                    });

                // If we hit execution bounds - return continuation: true.
                if (!isAccepted) {
                    response.setBody(responseBody);
                }
                return;
            }
        }
        // If the document array is empty, query for more documents.
        processQuery();
    }
}
